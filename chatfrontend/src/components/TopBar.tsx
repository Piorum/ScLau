import React from 'react';
import HamburgerIcon from '../icons/HamburgerIcon';
import './TopBar.css';

interface TopBarProps {
  onMenuToggle: () => void;
  title: string | null;
}

const TopBar: React.FC<TopBarProps> = ({ onMenuToggle, title }) => {
  return (
    <div className="top-bar">
      <button onClick={onMenuToggle} className="menu-toggle-button">
        <HamburgerIcon color="var(--color-text-main)" />
      </button>
      <div className="top-bar-title-bar">
        <h1 className="top-bar-title">{title}</h1>
      </div>
    </div>
  );
};

export default TopBar;
