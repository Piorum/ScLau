import React, { useState } from 'react';
import ReactMarkdown from 'react-markdown';
import './ChatMessage.css';
import CollapsibleMessage from './components/CollapsibleMessage';
import MessageActions from './components/MessageActions';
import EditableMessageContent from './components/EditableMessageContent';

export type Sender = 'user' | 'ai-reasoning' | 'ai-answer';

export interface Message {
  id: string;
  text: string;
  sender: Sender;
  isStreaming?: boolean;
}

interface ChatMessageProps {
  message: Message;
  deleteMessage: (messageId: string) => void;
  editMessage: (messageId: string, newText: string) => void;
}

const ChatMessage: React.FC<ChatMessageProps> = ({ message, deleteMessage, editMessage }) => {
  // isEditing state controls whether the message is currently in edit mode.
  const [isEditing, setIsEditing] = useState(false);

  const handleEdit = () => {
    setIsEditing(true);
  };

  const handleDelete = () => {
    deleteMessage(message.id);
  };

  // handleSave and handleCancel are passed to EditableMessageContent
  // to manage the editing lifecycle.
  const handleSave = (newText: string) => {
    editMessage(message.id, newText);
    setIsEditing(false);
  };

  const handleCancel = () => {
    setIsEditing(false);
  };

  // If the message is in edit mode, render the EditableMessageContent component.
  // This component encapsulates the textarea and save/cancel buttons.
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

  // If the message is an AI reasoning message, render the CollapsibleMessage component.
  // This component handles its own expanded/collapsed state and editing.
  if (message.sender === 'ai-reasoning') {
    return (
      <div className={`chat-message-wrapper ${message.sender}`}>
        <CollapsibleMessage 
          message={message}
          onEdit={editMessage}
          onDelete={deleteMessage}
        />
      </div>
    );
  }

  // For all other messages (user, AI answer), render the standard chat message.
  // Context actions (edit/delete) are shown only if the message is not currently streaming.
  return (
    <div className={`chat-message-wrapper ${message.sender}`}>
      <div className={`chat-message ${message.sender}`}>
        {!message.isStreaming && <MessageActions onEdit={handleEdit} onDelete={handleDelete} />}
        <ReactMarkdown>{message.text}</ReactMarkdown>
      </div>
    </div>
  );
};

export default ChatMessage;
