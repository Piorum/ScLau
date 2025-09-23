
import React from 'react';
import ReactMarkdown from 'react-markdown';
import './ChatMessage.css';
import CollapsibleMessage from './components/CollapsibleMessage';

export type Sender = 'user' | 'ai-reasoning' | 'ai-answer';

export interface Message {
  id: string;
  text: string;
  sender: Sender;
  isLoading?: boolean;
}

interface ChatMessageProps {
  message: Message;
}

const ChatMessage: React.FC<ChatMessageProps> = ({ message }) => {
  if (message.sender === 'ai-reasoning') {
    return (
      <div className={`chat-message-wrapper ${message.sender}`}>
        <CollapsibleMessage title="Reasoning" isLoading={!!message.isLoading}>
          <ReactMarkdown>{message.text}</ReactMarkdown>
        </CollapsibleMessage>
      </div>
    );
  }

  return (
    <div className={`chat-message-wrapper ${message.sender}`}>
      <div className={`chat-message ${message.sender}`}>
        <ReactMarkdown>{message.text}</ReactMarkdown>
      </div>
    </div>
  );
};

export default ChatMessage;
