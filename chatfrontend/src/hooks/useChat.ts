import { useReducer } from 'react';
import { Message } from '../ChatMessage';
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
  | { type: 'edit_message', payload: { messageId: string, newText: string } };


function messageReducer(state: ChatState, action: ReducerAction): ChatState {
  switch (action.type) {
    case 'delete_message':
      return { ...state, messages: state.messages.filter(m => m.id !== action.payload) };
    case 'edit_message':
        return {
            ...state,
            messages: state.messages.map(m =>
                m.id === action.payload.messageId ? { ...m, text: action.payload.newText } : m
            )
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

export const useChat = () => {
  const [state, dispatch] = useReducer(messageReducer, initialState);

  const sendMessage = async (inputValue: string) => {
    if (!inputValue.trim()) return;

    const userMessageId = crypto.randomUUID();
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
      const errorId = crypto.randomUUID();
      const errorMessage = `Error: ${error instanceof Error ? error.message : String(error)}`;
      dispatch({ type: 'reasoning_started', payload: { messageId: errorId, chunk: errorMessage } });
      dispatch({ type: 'stream_done' });
    }
  };

  const deleteMessage = (messageId: string) => {
    console.log('Simulating backend delete for messageId:', messageId);
    dispatch({ type: 'delete_message', payload: messageId });
  };

  const editMessage = (messageId: string, newText: string) => {
    console.log('Simulating backend edit for messageId:', messageId);
    dispatch({ type: 'edit_message', payload: { messageId, newText } });
  };

  return { messages: state.messages, isAiResponding: state.isAiResponding, sendMessage, deleteMessage, editMessage };
};