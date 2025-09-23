import React, { useState, useRef, useEffect } from 'react';
import './EditableMessageContent.css';

interface EditableMessageContentProps {
  initialText: string;
  onSave: (newText: string) => void;
  onCancel: () => void;
}

const EditableMessageContent: React.FC<EditableMessageContentProps> = ({ initialText, onSave, onCancel }) => {
  const [editedText, setEditedText] = useState(initialText);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  useEffect(() => {
    setEditedText(initialText);
  }, [initialText]);

  useEffect(() => {
    if (textareaRef.current) {
      textareaRef.current.focus();
      textareaRef.current.style.height = 'auto';
      textareaRef.current.style.height = `${textareaRef.current.scrollHeight}px`;
    }
  }, []); // Run once on mount

  const handleTextChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    setEditedText(e.target.value);
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
      textareaRef.current.style.height = `${textareaRef.current.scrollHeight}px`;
    }
  };

  return (
    <div className="editable-message-content">
      <textarea 
        ref={textareaRef}
        value={editedText} 
        onChange={handleTextChange} 
        className="edit-textarea"
      />
      <div className="edit-actions">
        <button onClick={() => onSave(editedText)}>Save</button>
        <button onClick={onCancel}>Cancel</button>
      </div>
    </div>
  );
};

export default EditableMessageContent;
