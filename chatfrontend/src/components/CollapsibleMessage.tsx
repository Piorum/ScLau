import React, { useState, useRef, useEffect } from 'react';
import LoadingIcon from './LoadingIcon';
import MessageActions from './MessageActions';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import remarkMath from 'remark-math';
import rehypeKatex from 'rehype-katex';
import rehypeHighlight from 'rehype-highlight';
import twemoji from 'twemoji';
import MultiPartEditableMessageContent from './MultiPartEditableMessageContent';
import './CollapsibleMessage.css';
import '../ChatMessage.css';
import { Message } from '../types';

interface CollapsibleMessageProps {
  message: Message;
  onEdit: (edits: { partId: string, newText: string }[]) => void;
  onDelete: () => void;
  onBranch: () => void;
  onRegenerate: () => void;
}

const PartRenderer: React.FC<{ text: string }> = ({ text }) => {
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => {
    if (ref.current) {
      twemoji.parse(ref.current, { folder: 'svg', ext: '.svg' });
    }
  }, [text]);

  return (
    <div ref={ref}>
      <ReactMarkdown
        remarkPlugins={[remarkGfm, remarkMath]}
        rehypePlugins={[rehypeKatex, rehypeHighlight]}
      >
        {text}
      </ReactMarkdown>
    </div>
  );
};

const CollapsibleMessage: React.FC<CollapsibleMessageProps> = ({ message, onEdit, onDelete, onBranch, onRegenerate }) => {
  const [isExpanded, setIsExpanded] = useState(false);
  const [isEditing, setIsEditing] = useState(false);

  const toggleExpand = () => {
    if (!isEditing) {
      setIsExpanded(!isExpanded);
    }
  };

  const handleEdit = () => {
    setIsEditing(true);
    setIsExpanded(true);
  };

  const handleDelete = () => {
    onDelete();
  };

  const handleSave = (edits: { partId: string, newText: string }[]) => {
    onEdit(edits);
    setIsEditing(false);
  };

  const handleCancel = () => {
    setIsEditing(false);
  };

  if (isEditing) {
    return (
      <div className={`chat-message ${message.sender} editing`}>
        <MultiPartEditableMessageContent
          parts={message.parts!}
          onSave={handleSave}
          onCancel={handleCancel}
        />
      </div>
    );
  }

  return (
    <div className={`collapsible-message ${isExpanded ? 'expanded' : ''}`}>
      <div className="collapsible-header" onClick={toggleExpand}>
        <div className="arrow"></div>
        <span>{message.sender === 'ai-reasoning' ? 'Reasoning' : message.sender}</span>
        {message.isStreaming && <LoadingIcon />}
      </div>
      <div className="collapsible-content">
        {message.parts?.map((part, index) => (
          <div key={part.id} style={{ borderBottom: index < message.parts!.length - 1 ? '1px solid var(--color-border)' : 'none' }}>
            <PartRenderer text={part.text} />
          </div>
        ))}
      </div>
      {isExpanded && !message.isStreaming && <MessageActions onEdit={handleEdit} onDelete={handleDelete} onBranch={onBranch} onRegenerate={onRegenerate} />}
    </div>
  );
};export default CollapsibleMessage;
