import React, { useState, useRef, useEffect } from 'react';
import './EditableMessageContent.css';

interface EditableMessageContentProps {
  initialText: string;
  onSave: (newText: string) => void;
  onCancel: () => void;
}

const EditableMessageContent: React.FC<EditableMessageContentProps> = ({ initialText, onSave, onCancel }) => {
  // editedText state holds the current value of the textarea.
  const [editedText, setEditedText] = useState(initialText);
  // textareaRef is used to directly access the textarea DOM element for auto-resizing.
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  // Syncs the editedText state with the initialText prop.
  // This ensures that if the message content changes externally (e.g., after an edit is saved),
  // the textarea reflects the latest message text.
  useEffect(() => {
    setEditedText(initialText);
  }, [initialText]);

  // Auto-resizes the textarea to fit its content when the component mounts.
  useEffect(() => {
    if (textareaRef.current) {
      textareaRef.current.focus();
      textareaRef.current.style.height = 'auto';
      textareaRef.current.style.height = `${textareaRef.current.scrollHeight}px`;
    }
  }, []); // Run once on mount

  // Handles changes to the textarea's value and auto-resizes it.
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