import React, { useState, useRef, useEffect } from 'react';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import remarkMath from 'remark-math';
import rehypeKatex from 'rehype-katex';
import rehypeHighlight from 'rehype-highlight';
import twemoji from 'twemoji';
import './ChatMessage.css';
import CollapsibleMessage from './components/CollapsibleMessage';
import MessageActions from './components/MessageActions';
import EditableMessageContent from './components/EditableMessageContent';
import { Message } from './types';

interface ChatMessageProps {
  message: Message;
  deleteMessage: (messageId: string | string[]) => void;
  editMessage: (messageId: string, newContent: string | { partId: string, newText: string }[]) => void;
  branch: (messageId: string) => void;
  regenerate: (messageId: string) => void;
}

const ChatMessage: React.FC<ChatMessageProps> = ({ message, deleteMessage, editMessage, branch, regenerate }) => {
  const [isEditing, setIsEditing] = useState(false);
  const [isSelected, setIsSelected] = useState(false);
  const [isMobile, setIsMobile] = useState(false);
  const contentRef = useRef<HTMLDivElement>(null);
  const messageRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const mediaQuery = window.matchMedia("(max-width: 768px)");
    setIsMobile(mediaQuery.matches);

    const handler = (e: MediaQueryListEvent) => setIsMobile(e.matches);
    mediaQuery.addEventListener('change', handler);

    return () => mediaQuery.removeEventListener('change', handler);
  }, []);

  useEffect(() => {
    if (contentRef.current) {
      twemoji.parse(contentRef.current, { folder: 'svg', ext: '.svg' });
    }
  }, [message.text, isEditing, message.isStreaming]);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (isMobile && messageRef.current && !messageRef.current.contains(event.target as Node)) {
        setIsSelected(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [isMobile, messageRef]);

  const handleEdit = () => {
    setIsEditing(true);
  };

  const handleDelete = () => {
    deleteMessage(message.id);
  };

  const handleBranch = () => {
    branch(message.id);
  };

  const handleRegenerate = () => {
    regenerate(message.id);
  };

  const handleSave = (newText: string) => {
    editMessage(message.id, newText);
    setIsEditing(false);
  };

  const handleCancel = () => {
    setIsEditing(false);
  };

  const handleWrapperClick = () => {
    if (isMobile) {
      setIsSelected(!isSelected);
    }
  };

  if (isEditing) {
    return (
      <div className={`chat-message-wrapper ${message.sender}`}>
        <div className={`chat-message ${message.sender} editing`}>
          <EditableMessageContent 
            initialText={message.text} 
            onSave={handleSave} 
            onCancel={handleCancel} 
          />
        </div>
      </div>
    );
  }

  if (message.sender === 'ai-reasoning') {
    const handleGroupEdit = (edits: { partId: string, newText: string }[]) => {
      editMessage(message.id, edits);
    };
    const handleGroupDelete = () => {
      const partIds = message.parts?.map(p => p.id) || [];
      if (partIds.length > 0) {
        deleteMessage(partIds);
      }
    };

    return (
      <div className={`chat-message-wrapper ${message.sender}`}>
        <CollapsibleMessage 
          message={message}
          onEdit={handleGroupEdit}
          onDelete={handleGroupDelete}
          onBranch={handleBranch}
          onRegenerate={handleRegenerate}
        />
      </div>
    );
  }

  return (
    <div className={`chat-message-wrapper ${message.sender}`} onClick={handleWrapperClick} ref={messageRef}>
      <div className={`chat-message ${message.sender} ${isMobile && isSelected ? 'selected' : ''}`}>
        {!message.isStreaming && <MessageActions onEdit={handleEdit} onDelete={handleDelete} onBranch={handleBranch} onRegenerate={handleRegenerate} />}
        <div ref={contentRef}>
          <ReactMarkdown
            remarkPlugins={[remarkGfm, remarkMath]}
            rehypePlugins={[rehypeKatex, rehypeHighlight]}
          >{message.text}</ReactMarkdown>
        </div>
      </div>
    </div>
  );
};

export default ChatMessage;
