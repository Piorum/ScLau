import React, { useState } from 'react';
import LoadingIcon from './LoadingIcon';
import MessageActions from './MessageActions';
import './CollapsibleMessage.css';

interface CollapsibleMessageProps {
  title: string;
  children: React.ReactNode;
  isStreaming: boolean;
  onEdit: () => void;
  onDelete: () => void;
}

const CollapsibleMessage: React.FC<CollapsibleMessageProps> = ({ title, children, isStreaming, onEdit, onDelete }) => {
  const [isExpanded, setIsExpanded] = useState(false);

  const toggleExpand = () => {
    setIsExpanded(!isExpanded);
  };

  return (
    <div className={`collapsible-message ${isExpanded ? 'expanded' : ''}`}>
      <div className="collapsible-header" onClick={toggleExpand}>
        <div className="arrow"></div>
        <span>{title}</span>
        {isStreaming && <LoadingIcon />} 
      </div>
      <div className="collapsible-content">
        {children}
      </div>
      {isExpanded && !isStreaming && <MessageActions onEdit={onEdit} onDelete={onDelete} />}
    </div>
  );
};
export default CollapsibleMessage;