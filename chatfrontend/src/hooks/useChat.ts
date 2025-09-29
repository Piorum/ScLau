import { useReducer } from 'react';
import { Message } from '../types';
import { streamChat, MessageStreamEvent } from '../utils/streamParser';

type ChatState = {
  messages: Message[];
  isAiResponding: boolean;
};

const initialState: ChatState = {
  messages: [],
  isAiResponding: false,
};

type ReducerAction =
  | MessageStreamEvent
  | { type: 'add_user_message'; payload: Message }
  | { type: 'set_is_responding'; payload: boolean }
  | { type: 'delete_message'; payload: string }
  | { type: 'delete_messages'; payload: string[] }
  | { type: 'edit_message', payload: { messageId: string, newText: string } }
  | { type: 'edit_reasoning_parts', payload: { edits: { partId: string, newText: string }[] } };


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

  const sendMessage = async (inputValue: string) => {
    if (!inputValue.trim()) return;

    const userMessageId = randomUUID();
    const userMessage: Message = {
      id: userMessageId,
      text: inputValue,
      sender: 'user',
    };
    dispatch({ type: 'add_user_message', payload: userMessage });
    dispatch({ type: 'set_is_responding', payload: true });

    try {
      const chatId = "0";
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
        dispatch(event);
      }
    } catch (error) {
      const errorId = randomUUID();
      const errorMessage = `Error: ${error instanceof Error ? error.message : String(error)}`;
      dispatch({ type: 'reasoning_started', payload: { messageId: errorId, chunk: errorMessage } });
      dispatch({ type: 'stream_done' });
    }
  };

  const deleteMessage = (messageId: string | string[]) => {
    if (Array.isArray(messageId)) {
      console.log('Simulating backend delete for messageIds:', messageId);
      dispatch({ type: 'delete_messages', payload: messageId });
    } else {
      console.log('Simulating backend delete for messageId:', messageId);
      dispatch({ type: 'delete_message', payload: messageId });
    }
  };

  const editMessage = (messageId: string, newContent: string | { partId: string, newText: string }[]) => {
    if (typeof newContent === 'string') {
      console.log('Simulating backend edit for messageId:', messageId);
      dispatch({ type: 'edit_message', payload: { messageId, newText: newContent } });
    } else {
      console.log('Simulating backend edit for reasoning parts of group:', messageId);
      dispatch({ type: 'edit_reasoning_parts', payload: { edits: newContent } });
    }
  };

  return { messages: state.messages, isAiResponding: state.isAiResponding, sendMessage, deleteMessage, editMessage };
};