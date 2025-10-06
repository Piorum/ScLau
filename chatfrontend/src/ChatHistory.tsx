import React, { useLayoutEffect, useRef, useEffect } from 'react';
import ChatMessage from './ChatMessage';
import LoadingIcon from './components/LoadingIcon';
import './ChatHistory.css';
import { Message } from './types';

function usePrevious<T>(value: T) {
    const ref = useRef<T | undefined>(undefined);
    useEffect(() => {
      ref.current = value;
    });
    return ref.current;
}

interface ChatHistoryProps {
  messages: Message[];
  isAiResponding: boolean;
  deleteMessage: (messageId: string | string[]) => void;
  editMessage: (messageId: string, newContent: string | { partId: string, newText: string }[]) => void;
  historyLoading: boolean;
  chatId: string | null;
}

const ChatHistory: React.FC<ChatHistoryProps> = ({ messages, isAiResponding, deleteMessage, editMessage, chatId }) => {
  const chatContainerRef = useRef<HTMLDivElement>(null);
  const userScrolled = useRef(false);
  const prevChatId = usePrevious(chatId);

  const handleWheel = () => {
    userScrolled.current = true;
  };

  useLayoutEffect(() => {
    const container = chatContainerRef.current;
    if (!container) return;

    const chatSwitched = prevChatId !== chatId;
    const lastMessage = messages[messages.length - 1];
    const isUserMessage = lastMessage?.sender === 'user';

    if (chatSwitched) {
      userScrolled.current = false;
    }
    if (isUserMessage) {
      userScrolled.current = false;
    }

    if (chatSwitched || isUserMessage || !userScrolled.current) {
      container.scrollTop = container.scrollHeight;
    }
  }, [messages, chatId, prevChatId]);

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
