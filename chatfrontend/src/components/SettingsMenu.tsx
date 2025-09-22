import React from 'react';
import ThemeToggler from './ThemeToggler';
import './SettingsMenu.css';

interface SettingsMenuProps {
  theme: string;
  onThemeToggle: () => void;
  onClose: () => void;
  isOpen: boolean;
}

const SettingsMenu: React.FC<SettingsMenuProps> = ({ theme, onThemeToggle, onClose, isOpen }) => {
  return (
    <>
        <div className={`settings-menu-overlay ${isOpen ? 'open' : ''}`} onClick={onClose}></div>
        <div className={`settings-menu ${isOpen ? 'open' : ''}`}>
            <div className="settings-menu-header">
                <h2>Settings</h2>
                <button onClick={onClose} className="close-button">&times;</button>
            </div>
            <div className="settings-menu-content">
                <ThemeToggler theme={theme} onToggle={onThemeToggle} />
            </div>
        </div>
    </>
  );
};

export default SettingsMenu;
