import React from 'react';
import EditIcon from '../icons/EditIcon';
import DeleteIcon from '../icons/DeleteIcon';
import './MessageActions.css';

interface MessageActionsProps {
  onEdit: () => void;
  onDelete: () => void;
}

const MessageActions: React.FC<MessageActionsProps> = ({ onEdit, onDelete }) => {
  return (
    <div className="message-actions">
      <button onClick={onEdit} title="Edit">
        <EditIcon />
      </button>
      <button onClick={onDelete} title="Delete">
        <DeleteIcon />
      </button>
    </div>
  );
};

export default MessageActions;
