import React, { useLayoutEffect, useRef } from 'react';
import ChatMessage, { Message } from './ChatMessage';
import LoadingIcon from './components/LoadingIcon';
import './ChatHistory.css';

interface ChatHistoryProps {
  messages: Message[];
  isAiResponding: boolean;
  deleteMessage: (messageId: string) => void;
  editMessage: (messageId: string, newText: string) => void;
}

const ChatHistory: React.FC<ChatHistoryProps> = ({ messages, isAiResponding, deleteMessage, editMessage }) => {
  const chatContainerRef = useRef<HTMLDivElement>(null);
  // userScrolled is a ref to track if the user has manually scrolled up.
  // This prevents auto-scrolling from interfering with the user's reading experience.
  const userScrolled = useRef(false);

  // handleWheel and handleTouchStart detect user scroll interaction.
  // If the user scrolls, auto-scrolling is paused until a new user message is sent.
  const handleWheel = () => {
    userScrolled.current = true;
  };

  // useLayoutEffect is used to perform DOM measurements and manipulations synchronously
  // after all DOM mutations, ensuring accurate scroll positioning.
  useLayoutEffect(() => {
    const container = chatContainerRef.current;
    if (!container) return;

    const lastMessage = messages[messages.length - 1];
    if (!lastMessage) return;

    const isUserMessage = lastMessage.sender === 'user';

    // If the last message is from the user, always snap to the bottom and reset the scroll flag.
    if (isUserMessage) {
      userScrolled.current = false;
      container.scrollTop = container.scrollHeight;
    } else {
      // For AI messages, scroll smoothly only if the user has not manually scrolled up.
      if (!userScrolled.current) {
        const lastElement = container.lastElementChild as HTMLElement;
        if (lastElement) {
          lastElement.scrollIntoView({ behavior: 'smooth', block: 'end' });
        }
      }
    }
  }, [messages]); // Re-run effect whenever messages array changes.

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
      {isAiResponding && <LoadingIcon />}
    </div>
  );
};

export default ChatHistory;