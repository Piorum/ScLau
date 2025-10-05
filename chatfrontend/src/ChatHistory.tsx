import React, { useLayoutEffect, useRef } from 'react';
import ChatMessage from './ChatMessage';
import LoadingIcon from './components/LoadingIcon';
import './ChatHistory.css';
import { Message } from './types';

interface ChatHistoryProps {
  messages: Message[];
  isAiResponding: boolean;
  deleteMessage: (messageId: string | string[]) => void;
  editMessage: (messageId: string, newContent: string | { partId: string, newText: string }[]) => void;
  historyLoading: boolean;
}

const ChatHistory: React.FC<ChatHistoryProps> = ({ messages, isAiResponding, deleteMessage, editMessage, historyLoading }) => {
  const chatContainerRef = useRef<HTMLDivElement>(null);
  const userScrolled = useRef(false);

  const handleWheel = () => {
    if (!historyLoading) {
      userScrolled.current = true;
    }
  };

  useLayoutEffect(() => {
    const container = chatContainerRef.current;
    if (!container) return;

    if (historyLoading) {
        container.scrollTop = container.scrollHeight;
        userScrolled.current = false;
        return;
    }

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
  }, [messages, historyLoading]);

  const groupedMessages: Message[] = [];
  let currentReasoningGroup: Message[] = [];

  for (const message of messages) {
    if (message.sender === 'ai-reasoning') {
      currentReasoningGroup.push(message);
    } else {
      if (currentReasoningGroup.length > 0) {
        groupedMessages.push({
          id: currentReasoningGroup[0].id,
          sender: 'ai-reasoning',
          text: '',
          parts: currentReasoningGroup.map(m => ({ id: m.id, text: m.text })),
          isStreaming: currentReasoningGroup.some(m => m.isStreaming),
        });
        currentReasoningGroup = [];
      }
      groupedMessages.push(message);
    }
  }

  if (currentReasoningGroup.length > 0) {
    groupedMessages.push({
      id: currentReasoningGroup[0].id,
      sender: 'ai-reasoning',
      text: '',
      parts: currentReasoningGroup.map(m => ({ id: m.id, text: m.text })),
      isStreaming: currentReasoningGroup.some(m => m.isStreaming),
    });
  }

  return (
    <div 
      className="chat-history" 
      ref={chatContainerRef} 
      onWheel={handleWheel}
      onTouchStart={handleWheel}
    >
      <div style={{ flexGrow: 1 }}></div>
      {groupedMessages.map((message) => (
        <ChatMessage key={message.id} message={message} deleteMessage={deleteMessage} editMessage={editMessage} />
      ))}
      {isAiResponding && <LoadingIcon />}
    </div>
  );
};

export default ChatHistory;