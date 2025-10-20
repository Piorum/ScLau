import React from 'react';
import ChatMessage from './ChatMessage';
import LoadingIcon from './components/LoadingIcon';
import './ChatHistory.css';
import { Message } from './types';

interface ChatHistoryProps {
  messages: Message[];
  isAiResponding: boolean;
  deleteMessage: (messageId: string | string[]) => void;
  editMessage: (messageId: string, newContent: string | { partId: string, newText: string }[]) => void;
  branch: (messageId: string) => void;
  regenerate: (messageId: string) => void;
  onScroll: (event: React.UIEvent<HTMLDivElement>) => void;
}

const ChatHistory = React.forwardRef<HTMLDivElement, ChatHistoryProps>(
  ({ messages, isAiResponding, deleteMessage, editMessage, branch, regenerate, onScroll }, ref) => {
  
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
      ref={ref}
      onScroll={onScroll}
    >
      {isAiResponding && <LoadingIcon />}
      {groupedMessages.slice().reverse().map((message) => (
        <ChatMessage key={message.id} message={message} deleteMessage={deleteMessage} editMessage={editMessage} branch={branch} regenerate={regenerate} />
      ))}
    </div>
  );
});

export default ChatHistory;
