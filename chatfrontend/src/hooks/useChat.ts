import { useReducer } from 'react';
import { Message } from '../ChatMessage';
import { streamChat, MessageStreamEvent } from '../utils/streamParser';

type ChatState = {
  messages: Message[];
};

const initialState: ChatState = {
  messages: [],
};

function messageReducer(state: ChatState, action: MessageStreamEvent | { type: 'add_user_message'; payload: Message }): ChatState {
  switch (action.type) {
    case 'add_user_message':
      return { ...state, messages: [...state.messages, action.payload] };
    case 'reasoning_started': {
      const newMessage: Message = {
        id: action.payload.id,
        text: action.payload.text,
        sender: 'ai-reasoning',
        isStreaming: true,
      };
      return { ...state, messages: [...state.messages, newMessage] };
    }
    case 'reasoning_append': {
      return {
        ...state,
        messages: state.messages.map(m =>
          m.id === action.payload.id ? { ...m, text: m.text + action.payload.text } : m
        ),
      };
    }
    case 'answer_started': {
      const newMessages = state.messages.map(m => 
        m.sender === 'ai-reasoning' ? { ...m, isStreaming: false } : m
      );
      const newMessage: Message = {
        id: action.payload.id,
        text: action.payload.text,
        sender: 'ai-answer',
        isStreaming: true,
      };
      return { ...state, messages: [...newMessages, newMessage] };
    }
    case 'answer_append': {
      return {
        ...state,
        messages: state.messages.map(m =>
          m.id === action.payload.id ? { ...m, text: m.text + action.payload.text } : m
        ),
      };
    }
    case 'stream_done': {
      return {
        ...state,
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

    const userMessage: Message = {
      id: Date.now().toString(),
      text: inputValue,
      sender: 'user',
    };
    dispatch({ type: 'add_user_message', payload: userMessage });

    try {
      const response = await fetch('/api/data', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(inputValue),
      });

      if (!response.ok || !response.body) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      for await (const event of streamChat(response.body.getReader())) {
        dispatch(event);
      }
    } catch (error) {
      const errorPayload = {
        id: (Date.now() + 3).toString(),
        text: `Error: ${error instanceof Error ? error.message : String(error)}`,
      };
      dispatch({ type: 'reasoning_started', payload: { ...errorPayload, id: (Date.now() + 3).toString() } });
      dispatch({ type: 'stream_done' });
    }
  };

  const deleteMessage = (messageId: string) => {
    // This will need to be adapted to the reducer pattern if complex logic is needed
    // For now, a simple filter on the state is fine
  };

  const editMessage = (messageId: string, newText: string) => {
    // This will also need to be adapted
  };

  return { messages: state.messages, sendMessage, deleteMessage, editMessage };
};
