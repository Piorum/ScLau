import React from 'react';
import DownArrowIcon from '../icons/DownArrowIcon';
import './ScrollToBottomButton.css';

interface ScrollToBottomButtonProps {
  onClick: () => void;
  isVisible: boolean;
}

const ScrollToBottomButton: React.FC<ScrollToBottomButtonProps> = ({ onClick, isVisible }) => {
  return (
    <button 
      className={`scroll-to-bottom-button ${isVisible ? 'visible' : ''}`}
      onClick={onClick}
      title="Scroll to bottom"
    >
      <DownArrowIcon />
    </button>
  );
};

export default ScrollToBottomButton;
