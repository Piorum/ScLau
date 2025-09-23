import React, { useState } from 'react';
import LoadingIcon from './LoadingIcon';
import './CollapsibleMessage.css';

interface CollapsibleMessageProps {
  title: string;
  children: React.ReactNode;
  isLoading: boolean;
}

const CollapsibleMessage: React.FC<CollapsibleMessageProps> = ({ title, children, isLoading }) => {
  const [isExpanded, setIsExpanded] = useState(false);

  const toggleExpand = () => {
    setIsExpanded(!isExpanded);
  };

  return (
    <div className={`collapsible-message ${isExpanded ? 'expanded' : ''}`}>
      <div className="collapsible-header" onClick={toggleExpand}>
        <div className="arrow"></div>
        <span>{title}</span>
        {isLoading && <LoadingIcon />}
      </div>
      <div className="collapsible-content">
        {children}
      </div>
    </div>
  );
};

export default CollapsibleMessage;
