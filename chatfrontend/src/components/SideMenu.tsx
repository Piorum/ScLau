import React, { useState } from 'react';
import SettingsIcon from '../icons/SettingsIcon';
import SettingsMenu from './SettingsMenu';
import './SideMenu.css';

interface SideMenuProps {
  isOpen: boolean;
  onClose: () => void;
  theme: string;
  onThemeToggle: () => void;
}

const SideMenu: React.FC<SideMenuProps> = ({ isOpen, onClose, theme, onThemeToggle }) => {
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);

  const openSettings = () => {
    setIsSettingsOpen(true);
  };

  const closeSettings = () => {
    setIsSettingsOpen(false);
  };

  return (
    <>
      <div className={`side-menu-overlay ${isOpen ? 'open' : ''}`} onClick={onClose}></div>
      <div className={`side-menu ${isOpen ? 'open' : ''}`}>
        <div className="side-menu-content">
          {/* Placeholder for future menu items */}
        </div>
        <div className="side-menu-footer">
          <button className="settings-button" onClick={openSettings}>
            <SettingsIcon color="var(--color-text-main)" />
            <span className="settings-text">Settings</span>
          </button>
        </div>
      </div>
      <SettingsMenu 
        isOpen={isSettingsOpen} 
        onClose={closeSettings} 
        theme={theme} 
        onThemeToggle={onThemeToggle} 
      />
    </>
  );
};

export default SideMenu;
