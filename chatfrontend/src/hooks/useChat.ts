import { useReducer, useCallback, useMemo } from 'react';
import { Message, ChatListItem, BackendMessage, Sender } from '../types';
import { streamChat, MessageStreamEvent } from '../utils/streamParser';

type ChatState = {
  chatMessages: { [key: string]: Message[] };
  activeChatId: string | null;
  isAiResponding: { [key: string]: boolean };
  chats: ChatListItem[];
  historyLoading: boolean;
};

const initialState: ChatState = {
  chatMessages: {},
  activeChatId: null,
  isAiResponding: {},
  chats: [],
  historyLoading: false,
};

type ReducerAction =
  | { type: 'stream_event', payload: MessageStreamEvent, chatId: string }
  | { type: 'add_user_message', payload: Message, chatId: string }
  | { type: 'set_is_responding', payload: boolean, chatId: string }
  | { type: 'delete_message', payload: string }
  | { type: 'delete_messages', payload: string[] }
  | { type: 'edit_message', payload: { messageId: string, newText: string } }
  | { type: 'edit_reasoning_parts', payload: { edits: { partId: string, newText: string }[] } }
  | { type: 'set_active_chat_id', payload: string | null }
  | { type: 'set_chats', payload: ChatListItem[] }
  | { type: 'add_chat', payload: ChatListItem }
  | { type: 'clear_chat_history', chatId: string }
  | { type: 'set_history_loading', payload: boolean };

function messageReducer(state: ChatState, action: ReducerAction): ChatState {
  switch (action.type) {
    case 'add_chat':
      return { ...state, chats: [action.payload, ...state.chats] };
    case 'delete_message':
      // This logic would need to be updated to find which chat the message belongs to if deleting from a non-active chat is desired.
      // For now, it assumes deletion only happens on the active chat.
      if (!state.activeChatId) return state;
      return {
        ...state,
        chatMessages: {
          ...state.chatMessages,
          [state.activeChatId]: state.chatMessages[state.activeChatId].filter(m => m.id !== action.payload)
        }
      };
    case 'delete_messages':
        if (!state.activeChatId) return state;
        return {
            ...state,
            chatMessages: {
                ...state.chatMessages,
                [state.activeChatId]: state.chatMessages[state.activeChatId].filter(m => !action.payload.includes(m.id))
            }
        };
    case 'edit_message':
        if (!state.activeChatId) return state;
        return {
            ...state,
            chatMessages: {
                ...state.chatMessages,
                [state.activeChatId]: state.chatMessages[state.activeChatId].map(m =>
                    m.id === action.payload.messageId ? { ...m, text: action.payload.newText } : m
                )
            }
        };
    case 'edit_reasoning_parts':
        if (!state.activeChatId) return state;
        return {
            ...state,
            chatMessages: {
                ...state.chatMessages,
                [state.activeChatId]: state.chatMessages[state.activeChatId].map(m => {
                    const edit = action.payload.edits.find(e => e.partId === m.id);
                    return edit ? { ...m, text: edit.newText } : m;
                })
            }
        };
    case 'set_is_responding':
      return {
        ...state,
        isAiResponding: { ...state.isAiResponding, [action.chatId]: action.payload }
      };
    case 'add_user_message': {
      const { chatId, payload } = action;
      const newMessages = [...(state.chatMessages[chatId] || []), payload];
      return {
        ...state,
        chatMessages: { ...state.chatMessages, [chatId]: newMessages },
      };
    }
    case 'stream_event': {
        const { chatId, payload: event } = action;
        const currentMessages = state.chatMessages[chatId] || [];
        let newMessages = currentMessages;
        let isResponding = state.isAiResponding[chatId] || false;

        switch (event.type) {
            case 'reasoning_started': {
                const newMessage: Message = { id: event.payload.messageId, text: event.payload.chunk, sender: 'ai-reasoning', isStreaming: true };
                newMessages = [...currentMessages, newMessage];
                isResponding = false;
                break;
            }
            case 'reasoning_append': {
                newMessages = currentMessages.map(m => m.id === event.payload.messageId ? { ...m, text: m.text + event.payload.chunk } : m);
                break;
            }
            case 'answer_started': {
                const streamingStoppedMsgs = currentMessages.map(m => m.sender === 'ai-reasoning' ? { ...m, isStreaming: false } : m);
                const newMessage: Message = { id: event.payload.messageId, text: event.payload.chunk, sender: 'ai-answer', isStreaming: true };
                newMessages = [...streamingStoppedMsgs, newMessage];
                break;
            }
            case 'answer_append': {
                newMessages = currentMessages.map(m => m.id === event.payload.messageId ? { ...m, text: m.text + event.payload.chunk } : m);
                break;
            }
            case 'stream_done': {
                newMessages = currentMessages.map(m => ({ ...m, isStreaming: false }));
                isResponding = false;
                break;
            }
        }
        return {
            ...state,
            chatMessages: { ...state.chatMessages, [chatId]: newMessages },
            isAiResponding: { ...state.isAiResponding, [chatId]: isResponding }
        };
    }
    case 'set_active_chat_id':
      return { ...state, activeChatId: action.payload };
    case 'set_chats':
      return { ...state, chats: action.payload };
    case 'clear_chat_history':
        return {
            ...state,
            chatMessages: { ...state.chatMessages, [action.chatId]: [] }
        };
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

  const messages = useMemo(() => 
    state.activeChatId ? state.chatMessages[state.activeChatId] || [] : [],
    [state.activeChatId, state.chatMessages]
  );

  const isAiResponding = useMemo(() =>
    state.activeChatId ? state.isAiResponding[state.activeChatId] || false : false,
    [state.activeChatId, state.isAiResponding]
  );

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
    dispatch({ type: 'set_active_chat_id', payload: chatId });
    if (state.chatMessages[chatId]) {
        return; // Already loaded
    }

    dispatch({ type: 'clear_chat_history', chatId });
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

            dispatch({ type: 'add_user_message', payload: message, chatId });
          } catch (e) {
            console.error("Error parsing JSON line:", e, "Line was:", line);
          }
        }
      }

    } catch (error) {
      console.error(`Failed to load chat history for ${chatId}:`, error);
      dispatch({ type: 'set_history_loading', payload: false });
    }
  }, [state.chatMessages]);

  const startNewChat = useCallback(() => {
    dispatch({ type: 'set_active_chat_id', payload: null });
  }, []);

  const sendMessage = useCallback(async (inputValue: string) => {
    if (!inputValue.trim()) return;

    let chatId = state.activeChatId;
    if (!chatId) {
        chatId = randomUUID();
        dispatch({ type: 'set_active_chat_id', payload: chatId });
        const newChatItem: ChatListItem = {
            chatId: chatId,
            lastMessage: Math.floor(Date.now() / 1000),
        };
        dispatch({ type: 'add_chat', payload: newChatItem });
    }

    const userMessageId = randomUUID();
    const userMessage: Message = {
      id: userMessageId,
      text: inputValue,
      sender: 'user',
    };
    dispatch({ type: 'add_user_message', payload: userMessage, chatId });
    dispatch({ type: 'set_is_responding', payload: true, chatId });

    try {
      const response = await fetch(`/api/chats/${chatId}/messages`, {
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
        dispatch({ type: 'stream_event', payload: event, chatId });
      }
      loadChats();
    } catch (error) {
      const errorId = randomUUID();
      const errorMessage = `Error: ${error instanceof Error ? error.message : String(error)}`;
      const event: MessageStreamEvent = { type: 'reasoning_started', payload: { messageId: errorId, chunk: errorMessage } };
      dispatch({ type: 'stream_event', payload: event, chatId });
      const doneEvent: MessageStreamEvent = { type: 'stream_done' };
      dispatch({ type: 'stream_event', payload: doneEvent, chatId });
    }
  }, [state.activeChatId, loadChats]);

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

  return {
    chats: state.chats,
    activeChatId: state.activeChatId,
    historyLoading: state.historyLoading,
    messages, 
    isAiResponding, 
    sendMessage, 
    deleteMessage, 
    editMessage, 
    loadChats, 
    loadChatHistory, 
    startNewChat 
  };
};