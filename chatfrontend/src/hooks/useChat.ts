import { useReducer, useCallback } from 'react';
import { Message, ChatListItem, BackendMessage, Sender } from '../types';
import { streamChat, MessageStreamEvent } from '../utils/streamParser';

type ChatState = {
  messages: Message[];
  isAiResponding: boolean;
  chatId: string | null;
  chats: ChatListItem[];
  historyLoading: boolean;
};

const initialState: ChatState = {
  messages: [],
  isAiResponding: false,
  chatId: null,
  chats: [],
  historyLoading: false,
};

type ReducerAction =
  | MessageStreamEvent
  | { type: 'add_user_message'; payload: Message }
  | { type: 'set_is_responding'; payload: boolean }
  | { type: 'delete_message'; payload: string }
  | { type: 'delete_messages'; payload: string[] }
  | { type: 'edit_message', payload: { messageId: string, newText: string } }
  | { type: 'edit_reasoning_parts', payload: { edits: { partId: string, newText: string }[] } }
  | { type: 'set_chat_id'; payload: string | null }
  | { type: 'set_chats'; payload: ChatListItem[] }
  | { type: 'set_chat_history'; payload: Message[] }
  | { type: 'clear_messages' }
  | { type: 'set_history_loading'; payload: boolean };

function messageReducer(state: ChatState, action: ReducerAction): ChatState {
  switch (action.type) {
    case 'delete_message':
      return { ...state, messages: state.messages.filter(m => m.id !== action.payload) };
    case 'delete_messages':
      return { ...state, messages: state.messages.filter(m => !action.payload.includes(m.id)) };
    case 'edit_message':
        return {
            ...state,
            messages: state.messages.map(m =>
                m.id === action.payload.messageId ? { ...m, text: action.payload.newText } : m
            )
        };
    case 'edit_reasoning_parts':
        return {
            ...state,
            messages: state.messages.map(m => {
                const edit = action.payload.edits.find(e => e.partId === m.id);
                return edit ? { ...m, text: edit.newText } : m;
            })
        };
    case 'set_is_responding':
      return { ...state, isAiResponding: action.payload };
    case 'add_user_message':
      return { ...state, messages: [...state.messages, action.payload] };
    case 'reasoning_started': {
      const newMessage: Message = {
        id: action.payload.messageId,
        text: action.payload.chunk,
        sender: 'ai-reasoning',
        isStreaming: true,
      };
      return { ...state, isAiResponding: false, messages: [...state.messages, newMessage] };
    }
    case 'reasoning_append': {
      return {
        ...state,
        messages: state.messages.map(m =>
          m.id === action.payload.messageId ? { ...m, text: m.text + action.payload.chunk } : m
        ),
      };
    }
    case 'answer_started': {
      const newMessages = state.messages.map(m =>
        m.sender === 'ai-reasoning' ? { ...m, isStreaming: false } : m
      );
      const newMessage: Message = {
        id: action.payload.messageId,
        text: action.payload.chunk,
        sender: 'ai-answer',
        isStreaming: true,
      };
      return { ...state, messages: [...newMessages, newMessage] };
    }
    case 'answer_append': {
      return {
        ...state,
        messages: state.messages.map(m =>
          m.id === action.payload.messageId ? { ...m, text: m.text + action.payload.chunk } : m
        ),
      };
    }
    case 'stream_done': {
      return {
        ...state,
        isAiResponding: false,
        messages: state.messages.map(m => ({ ...m, isStreaming: false })),
      };
    }
    case 'set_chat_id':
      return { ...state, chatId: action.payload };
    case 'set_chats':
      return { ...state, chats: action.payload };
    case 'set_chat_history':
      return { ...state, messages: action.payload };
    case 'clear_messages':
      return { ...state, messages: [] };
    case 'set_history_loading':
      return { ...state, historyLoading: action.payload };
    default:
      return state;
  }
}

function randomUUID(): string {
  if (typeof crypto.randomUUID === "function") {
    try {
      return crypto.randomUUID();
    } catch {
      // falls through if blocked due to insecure context
    }
  }

  const buf = new Uint8Array(16);
  crypto.getRandomValues(buf);

  // RFC4122 v4 adjustments
  buf[6] = (buf[6] & 0x0f) | 0x40;
  buf[8] = (buf[8] & 0x3f) | 0x80;

  const hex = Array.from(buf)
    .map(b => b.toString(16).padStart(2, "0"))
    .join("");

  return (
    hex.slice(0, 8) + "-" +
    hex.slice(8, 12) + "-" +
    hex.slice(12, 16) + "-" +
    hex.slice(16, 20) + "-" +
    hex.slice(20)
  );
}


export const useChat = () => {
  const [state, dispatch] = useReducer(messageReducer, initialState);

  const loadChats = useCallback(async () => {
    try {
      const response = await fetch('/api/chats');
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      const chats: ChatListItem[] = await response.json();
      dispatch({ type: 'set_chats', payload: chats });
    } catch (error) {
      console.error("Failed to load chats:", error);
    }
  }, []);

  const loadChatHistory = useCallback(async (chatId: string) => {
    dispatch({ type: 'clear_messages' });
    dispatch({ type: 'set_chat_id', payload: chatId });
    dispatch({ type: 'set_history_loading', payload: true });
    try {
      const response = await fetch(`/api/chats/${chatId}`);
      if (!response.ok || !response.body) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const reader = response.body.getReader();
      const decoder = new TextDecoder();
      let buffer = '';

      while (true) {
        const { done, value } = await reader.read();
        if (done) {
          dispatch({ type: 'set_history_loading', payload: false });
          break;
        }

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n');
        buffer = lines.pop() || '';

        for (const line of lines) {
          if (!line.trim()) continue;

          try {
            const msg: BackendMessage = JSON.parse(line);
            
            let sender: Sender = 'ai-answer';
            if (msg.Role === 0) { // User
                sender = 'user';
            } else if (msg.Role === 1) { // Assistant
                if (msg.ContentType === 0) { // Reasoning
                    sender = 'ai-reasoning';
                } else { // Answer
                    sender = 'ai-answer';
                }
            } else if (msg.Role === 2) { // Tool
                sender = 'ai-reasoning';
            }

            const message: Message = {
                id: msg.MessageId,
                text: msg.Content,
                sender: sender,
            };

            dispatch({ type: 'add_user_message', payload: message });
          } catch (e) {
            console.error("Error parsing JSON line:", e, "Line was:", line);
          }
        }
      }

    } catch (error) {
      console.error(`Failed to load chat history for ${chatId}:`, error);
      dispatch({ type: 'set_history_loading', payload: false });
    }
  }, []);

  const startNewChat = useCallback(() => {
    dispatch({ type: 'clear_messages' });
    dispatch({ type: 'set_chat_id', payload: null });
  }, []);

  const sendMessage = useCallback(async (inputValue: string) => {
    if (!inputValue.trim()) return;

    const userMessageId = randomUUID();
    const userMessage: Message = {
      id: userMessageId,
      text: inputValue,
      sender: 'user',
    };
    dispatch({ type: 'add_user_message', payload: userMessage });
    dispatch({ type: 'set_is_responding', payload: true });

    let currentChatId = state.chatId;
    if (!currentChatId) {
      currentChatId = randomUUID();
      dispatch({ type: 'set_chat_id', payload: currentChatId });
    }

    try {
      const response = await fetch(`/api/chats/${currentChatId}/messages`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          userPrompt: inputValue,
          userMessageId: userMessageId,
        }),
      });

      if (!response.ok || !response.body) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      for await (const event of streamChat(response.body.getReader())) {
        dispatch(event);
      }
      loadChats();
    } catch (error) {
      const errorId = randomUUID();
      const errorMessage = `Error: ${error instanceof Error ? error.message : String(error)}`;
      dispatch({ type: 'reasoning_started', payload: { messageId: errorId, chunk: errorMessage } });
      dispatch({ type: 'stream_done' });
    }
  }, [state.chatId, loadChats]);

  const deleteMessage = useCallback((messageId: string | string[]) => {
    if (Array.isArray(messageId)) {
      console.log('Simulating backend delete for messageIds:', messageId);
      dispatch({ type: 'delete_messages', payload: messageId });
    } else {
      console.log('Simulating backend delete for messageId:', messageId);
      dispatch({ type: 'delete_message', payload: messageId });
    }
  }, []);

  const editMessage = useCallback((messageId: string, newContent: string | { partId: string, newText: string }[]) => {
    if (typeof newContent === 'string') {
      console.log('Simulating backend edit for messageId:', messageId);
      dispatch({ type: 'edit_message', payload: { messageId, newText: newContent } });
    } else {
      console.log('Simulating backend edit for reasoning parts of group:', messageId);
      dispatch({ type: 'edit_reasoning_parts', payload: { edits: newContent } });
    }
  }, []);

  return { ...state, sendMessage, deleteMessage, editMessage, loadChats, loadChatHistory, startNewChat };
};
