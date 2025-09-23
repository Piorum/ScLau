import React, { useLayoutEffect, useRef } from 'react';
import ChatMessage, { Message } from './ChatMessage';
import './ChatHistory.css';

interface ChatHistoryProps {
  messages: Message[];
  deleteMessage: (messageId: string) => void;
  editMessage: (messageId: string, newText: string) => void;
}

const ChatHistory: React.FC<ChatHistoryProps> = ({ messages, deleteMessage, editMessage }) => {
  const chatContainerRef = useRef<HTMLDivElement>(null);
  const userScrolled = useRef(false);

  const handleWheel = () => {
    userScrolled.current = true;
  };

  useLayoutEffect(() => {
    const container = chatContainerRef.current;
    if (!container) return;

    const lastMessage = messages[messages.length - 1];
    if (!lastMessage) return;

    const isUserMessage = lastMessage.sender === 'user';

    if (isUserMessage) {
      userScrolled.current = false;
      container.scrollTop = container.scrollHeight;
    } else {
      if (!userScrolled.current) {
        const lastElement = container.lastElementChild as HTMLElement;
        if (lastElement) {
          lastElement.scrollIntoView({ behavior: 'smooth', block: 'end' });
        }
      }
    }
  }, [messages]);

  return (
    <div 
      className="chat-history" 
      ref={chatContainerRef} 
      onWheel={handleWheel}
      onTouchStart={handleWheel}
    >
      <div style={{ flexGrow: 1 }}></div>
      {messages.map((message, index) => {
        return <ChatMessage key={message.id} message={{...message}} deleteMessage={deleteMessage} editMessage={editMessage} />;
      })}
    </div>
  );
};

export default ChatHistory;
