import React, { useEffect, useRef } from 'react';
import ChatMessage, { Message } from './ChatMessage';
import './ChatHistory.css';

interface ChatHistoryProps {
  messages: Message[];
}

const ChatHistory: React.FC<ChatHistoryProps> = ({ messages }) => {
  const chatEndRef = useRef<HTMLDivElement>(null);
  const chatContainerRef = useRef<HTMLDivElement>(null);
  const shouldScrollRef = useRef(true);

  useEffect(() => {
    if (shouldScrollRef.current) {
      chatEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }
  }, [messages]);

  const handleScroll = () => {
    const container = chatContainerRef.current;
    if (container) {
      const atBottom = container.scrollHeight - container.scrollTop - container.clientHeight < 50;
      shouldScrollRef.current = atBottom;
    }
  };
  
  // When user sends a message, we should scroll.
  useEffect(() => {
      if (messages.length > 0 && messages[messages.length - 1].sender === 'user') {
          shouldScrollRef.current = true;
      }
  }, [messages]);

  return (
    <div className="chat-history" ref={chatContainerRef} onScroll={handleScroll}>
      <div style={{ flexGrow: 1 }}></div>
      {messages.map((message, index) => {
        const isLoading = message.sender === 'ai-reasoning' && index === messages.length - 1;
        return <ChatMessage key={message.id} message={{...message, isLoading}} />;
      })}
    </div>
  );
};

export default ChatHistory;
