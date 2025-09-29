import React, { useState, useRef, useEffect } from 'react';
import { MessagePart } from '../types';
import './EditableMessageContent.css';

interface MultiPartEditableMessageContentProps {
  parts: MessagePart[];
  onSave: (edits: { partId: string, newText: string }[]) => void;
  onCancel: () => void;
}

const MultiPartEditableMessageContent: React.FC<MultiPartEditableMessageContentProps> = ({ parts, onSave, onCancel }) => {
  const [editedParts, setEditedParts] = useState(parts.map(p => ({ ...p })));
  const textareaRefs = useRef<(HTMLTextAreaElement | null)[]>([]);

  useEffect(() => {
    textareaRefs.current = textareaRefs.current.slice(0, parts.length);
  }, [parts]);

  useEffect(() => {
    editedParts.forEach((_, index) => {
      const textarea = textareaRefs.current[index];
      if (textarea) {
        textarea.style.height = 'auto';
        textarea.style.height = `${textarea.scrollHeight}px`;
      }
    });
  }, [editedParts]);

  const handleTextChange = (partId: string, newText: string) => {
    setEditedParts(currentParts =>
      currentParts.map(p => (p.id === partId ? { ...p, text: newText } : p))
    );
  };

  const handleSave = () => {
    onSave(editedParts.map(p => ({ partId: p.id, newText: p.text })));
  };

  return (
    <div className="editable-message-content"> 
      {editedParts.map((part, index) => (
        <div key={part.id} style={{marginBottom: '10px'}}>
          <textarea
            ref={el => { textareaRefs.current[index] = el; }}
            value={part.text}
            onChange={(e) => handleTextChange(part.id, e.target.value)}
            className="edit-textarea"
            rows={1}
          />
        </div>
      ))}
      <div className="edit-actions">
        <button onClick={handleSave}>Save</button>
        <button onClick={onCancel}>Cancel</button>
      </div>
    </div>
  );
};

export default MultiPartEditableMessageContent;
