
import React from 'react';
import ReactMarkdown from 'react-markdown';
import './ChatMessage.css';

export type Sender = 'user' | 'ai-reasoning' | 'ai-answer';

export interface Message {
  id: string;
  text: string;
  sender: Sender;
}

interface ChatMessageProps {
  message: Message;
}

const ChatMessage: React.FC<ChatMessageProps> = ({ message }) => {
  return (
    <div className={`chat-message-wrapper ${message.sender}`}>
      <div className={`chat-message ${message.sender}`}>
        <ReactMarkdown>{message.text}</ReactMarkdown>
      </div>
    </div>
  );
};

export default ChatMessage;
