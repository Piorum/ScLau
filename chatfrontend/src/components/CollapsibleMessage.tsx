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
  // isExpanded state controls the visibility of the message content.
  // It persists across re-renders, ensuring the message stays expanded/collapsed as chosen by the user.
  const [isExpanded, setIsExpanded] = useState(false);
  // isEditing state controls whether the message is currently in edit mode.
  const [isEditing, setIsEditing] = useState(false);

  // Toggles the expanded state of the message.
  const toggleExpand = () => {
    setIsExpanded(!isExpanded);
  };

  // Sets the message to edit mode.
  const handleEdit = () => {
    setIsEditing(true);
  };

  // Calls the onDelete prop to delete the message.
  const handleDelete = () => {
    onDelete(message.id);
  };

  // Calls the onEdit prop to save the edited text and exits edit mode.
  const handleSave = (newText: string) => {
    onEdit(message.id, newText);
    setIsEditing(false);
  };

  // Exits edit mode without saving changes.
  const handleCancel = () => {
    setIsEditing(false);
  };

  // Conditionally renders the message content based on whether it's in edit mode.
  const renderContent = () => {
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
        {/* Display 'Reasoning' for AI reasoning messages, otherwise the sender. */}
        <span>{message.sender === 'ai-reasoning' ? 'Reasoning' : message.sender}</span>
        {/* Show loading icon only if the message is currently streaming. */}
        {message.isStreaming && <LoadingIcon />} 
      </div>
      {/* Render the message content (view or edit mode). */}
      {renderContent()}
      {/* Show message actions (edit/delete) only if expanded and not streaming. */}
      {isExpanded && !message.isStreaming && <MessageActions onEdit={handleEdit} onDelete={handleDelete} />}
    </div>
  );
};
export default CollapsibleMessage;
