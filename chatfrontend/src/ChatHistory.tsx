import React, { useEffect, useRef } from 'react';
import ChatMessage, { Message } from './ChatMessage';
import './ChatHistory.css';

interface ChatHistoryProps {
  messages: Message[];
}

const ChatHistory: React.FC<ChatHistoryProps> = ({ messages }) => {
  const chatEndRef = useRef<HTMLDivElement>(null);

  const scrollToBottom = () => {
    chatEndRef.current?.scrollIntoView({ behavior: "smooth" });
  };

  useEffect(scrollToBottom, [messages]);

  return (
    <div className="chat-history">
      <div style={{ flexGrow: 1 }}></div>
      {messages.map((message) => (
        <ChatMessage key={message.id} message={message} />
      ))}
      <div ref={chatEndRef} />
    </div>
  );
};

export default ChatHistory;
