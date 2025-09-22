import React from 'react';
import HamburgerIcon from '../icons/HamburgerIcon';
import './TopBar.css';

interface TopBarProps {
  onMenuToggle: () => void;
}

const TopBar: React.FC<TopBarProps> = ({ onMenuToggle }) => {
  return (
    <div className="top-bar">
      <button onClick={onMenuToggle} className="menu-toggle-button">
        <HamburgerIcon color="var(--color-text-main)" />
      </button>
    </div>
  );
};

export default TopBar;
