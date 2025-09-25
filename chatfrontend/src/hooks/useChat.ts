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

type ReducerAction = MessageStreamEvent | { type: 'add_user_message'; payload: Message } | { type: 'set_is_responding'; payload: boolean } | { type: 'delete_message'; payload: string };

function messageReducer(state: ChatState, action: ReducerAction): ChatState {
  switch (action.type) {
    case 'delete_message':
      return { ...state, messages: state.messages.filter(m => m.id !== action.payload) };
    case 'set_is_responding':
      return { ...state, isAiResponding: action.payload };
    case 'add_user_message':
      return { ...state, messages: [...state.messages, action.payload] };
    case 'reasoning_started': {
      const newMessage: Message = {
        id: action.payload.id,
        text: action.payload.text,
        sender: 'ai-reasoning',
        isStreaming: true,
        chatIds: [action.payload.chatId],
      };
      return { ...state, isAiResponding: false, messages: [...state.messages, newMessage] };
    }
    case 'reasoning_append': {
      return {
        ...state,
        messages: state.messages.map(m =>
          m.id === action.payload.id ? { ...m, text: m.text + action.payload.text } : m
        ),
      };
    }
    case 'reasoning_add_chat_id': {
      return {
        ...state,
        messages: state.messages.map(m =>
          m.id === action.payload.id
            ? { ...m, chatIds: [...(m.chatIds || []), action.payload.chatId] }
            : m
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
        chatIds: [action.payload.chatId],
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

    const userMessageId = BigInt(Date.now());
    const userMessage: Message = {
      id: userMessageId.toString(),
      userMessageId,
      text: inputValue,
      sender: 'user',
    };
    dispatch({ type: 'add_user_message', payload: userMessage });
    dispatch({ type: 'set_is_responding', payload: true });

    try {
      const response = await fetch('/api/data', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          userPrompt: inputValue,
          userMessageId: userMessageId.toString(),
        }),
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
      dispatch({ type: 'reasoning_started', payload: { ...errorPayload, id: (Date.now() + 3).toString(), chatId: (Date.now() + 3).toString() } });
      dispatch({ type: 'stream_done' });
    }
  };

  const deleteMessage = (messageId: string) => {
    const messageToDelete = state.messages.find(m => m.id === messageId);
    if (messageToDelete && messageToDelete.chatIds) {
      console.log('Simulating backend delete for chatIds:', messageToDelete.chatIds);
    }
    dispatch({ type: 'delete_message', payload: messageId });
  };

  const editMessage = (messageId: string, newText: string) => {
    // This will also need to be adapted
  };

  return { messages: state.messages, isAiResponding: state.isAiResponding, sendMessage, deleteMessage, editMessage };
};
