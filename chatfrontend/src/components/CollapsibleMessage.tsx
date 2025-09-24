import React, { useState } from 'react';
import LoadingIcon from './LoadingIcon';
import MessageActions from './MessageActions';
import ReactMarkdown from 'react-markdown';
import EditableMessageContent from './EditableMessageContent';
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

  const toggleExpand = () => {
    if (!isEditing) {
      setIsExpanded(!isExpanded);
    }
  };

  const handleEdit = () => {
    setIsEditing(true);
  };

  const handleDelete = () => {
    onDelete(message.id);
  };

  const handleSave = (newText: string) => {
    onEdit(message.id, newText);
    setIsEditing(false);
  };

  const handleCancel = () => {
    setIsEditing(false);
  };

  if (isEditing) {
    return (
      <div className={`chat-message ${message.sender} editing`}>
        <EditableMessageContent 
          initialText={message.text} 
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
        <ReactMarkdown>{message.text}</ReactMarkdown>
      </div>
      {isExpanded && !message.isStreaming && <MessageActions onEdit={handleEdit} onDelete={handleDelete} />}
    </div>
  );
};export default CollapsibleMessage;
