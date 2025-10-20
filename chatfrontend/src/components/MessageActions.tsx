import React from 'react';
import EditIcon from '../icons/EditIcon';
import DeleteIcon from '../icons/DeleteIcon';
import BranchIcon from '../icons/BranchIcon';
import RegenerateIcon from '../icons/RegenerateIcon';
import './MessageActions.css';

interface MessageActionsProps {
  onEdit: () => void;
  onDelete: () => void;
  onBranch: () => void;
  onRegenerate: () => void;
}

const MessageActions: React.FC<MessageActionsProps> = ({ onEdit, onDelete, onBranch, onRegenerate }) => {
  return (
    <div className="message-actions">
      <button onClick={onBranch} title="Branch">
        <BranchIcon />
      </button>
      <button onClick={onRegenerate} title="Regenerate">
        <RegenerateIcon />
      </button>
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
