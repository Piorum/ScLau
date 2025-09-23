import React, { useState, useRef, useEffect } from 'react';
import ReactMarkdown from 'react-markdown';
import './ChatMessage.css';
import CollapsibleMessage from './components/CollapsibleMessage';
import MessageActions from './components/MessageActions';

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

  const handleEdit = () => {
    setIsEditing(true);
  };

  const handleDelete = () => {
    deleteMessage(message.id);
  };

  const handleSave = () => {
    editMessage(message.id, editedText);
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

  if (isEditing) {
    return (
      <div className={`chat-message-wrapper ${message.sender}`}>
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
      </div>
    );
  }

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
