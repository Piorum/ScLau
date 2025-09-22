import React from 'react';
import SettingsIcon from '../icons/SettingsIcon';
import './SideMenu.css';

interface SideMenuProps {
  isOpen: boolean;
  onClose: () => void;
  onSettingsClick: () => void;
}

const SideMenu: React.FC<SideMenuProps> = ({ isOpen, onClose, onSettingsClick }) => {
  return (
    <>
      <div className={`side-menu-overlay ${isOpen ? 'open' : ''}`} onClick={onClose}></div>
      <div className={`side-menu ${isOpen ? 'open' : ''}`}>
        <div className="side-menu-content">
          {/* Placeholder for future menu items */}
        </div>
        <div className="side-menu-footer">
          <button className="settings-button" onClick={onSettingsClick}>
            <SettingsIcon color="var(--color-text-main)" />
            <span className="settings-text">Settings</span>
          </button>
        </div>
      </div>
    </>
  );
};

export default SideMenu;
