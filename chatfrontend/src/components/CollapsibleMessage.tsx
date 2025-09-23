import React, { useState, useRef, useEffect } from 'react';
import LoadingIcon from './LoadingIcon';
import MessageActions from './MessageActions';
import ReactMarkdown from 'react-markdown';
import './CollapsibleMessage.css';
import '../ChatMessage.css'; // Import ChatMessage.css for edit styles
import { Message } from '../ChatMessage'; // Import Message interface

interface CollapsibleMessageProps {
  message: Message;
  onEdit: (messageId: string, newText: string) => void;
  onDelete: (messageId: string) => void;
}

const CollapsibleMessage: React.FC<CollapsibleMessageProps> = ({ message, onEdit, onDelete }) => {
  const [isExpanded, setIsExpanded] = useState(false);
  const [isEditing, setIsEditing] = useState(false);
  const [editedText, setEditedText] = useState(message.text);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  useEffect(() => {
    if (!isEditing) {
      setEditedText(message.text);
    }
  }, [message.text, isEditing]);

  useEffect(() => {
    if (isEditing && textareaRef.current) {
      textareaRef.current.focus();
      textareaRef.current.style.height = 'auto';
      textareaRef.current.style.height = `${textareaRef.current.scrollHeight}px`;
    }
  }, [isEditing]);

  const toggleExpand = () => {
    setIsExpanded(!isExpanded);
  };

  const handleEdit = () => {
    setIsEditing(true);
  };

  const handleDelete = () => {
    onDelete(message.id);
  };

  const handleSave = () => {
    onEdit(message.id, editedText);
    setIsEditing(false);
  };

  const handleCancel = () => {
    setEditedText(message.text);
    setIsEditing(false);
  };

  const handleTextChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    setEditedText(e.target.value);
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
      textareaRef.current.style.height = `${textareaRef.current.scrollHeight}px`;
    }
  };

  const renderContent = () => {
    if (isEditing) {
      return (
        <div className={`chat-message ${message.sender} editing`}>
          <textarea 
            ref={textareaRef}
            value={editedText} 
            onChange={handleTextChange} 
            className="edit-textarea"
          />
          <div className="edit-actions">
            <button onClick={handleSave}>Save</button>
            <button onClick={handleCancel}>Cancel</button>
          </div>
        </div>
      );
    } else {
      return (
        <div className="collapsible-content">
          <ReactMarkdown>{message.text}</ReactMarkdown>
        </div>
      );
    }
  };

  return (
    <div className={`collapsible-message ${isExpanded ? 'expanded' : ''}`}>
      <div className="collapsible-header" onClick={toggleExpand}>
        <div className="arrow"></div>
        <span>{message.sender === 'ai-reasoning' ? 'Reasoning' : message.sender}</span>
        {message.isStreaming && <LoadingIcon />} 
      </div>
      {renderContent()}
      {isExpanded && !message.isStreaming && <MessageActions onEdit={handleEdit} onDelete={handleDelete} />}
    </div>
  );
};
export default CollapsibleMessage;
