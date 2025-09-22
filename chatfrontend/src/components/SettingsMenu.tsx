import React from 'react';
import ThemeToggler from './ThemeToggler';
import './SettingsMenu.css';
import { useTheme } from '../context/ThemeContext';

interface SettingsMenuProps {
  onClose: () => void;
  isOpen: boolean;
}

const SettingsMenu: React.FC<SettingsMenuProps> = ({ onClose, isOpen }) => {
  const { theme, toggleTheme } = useTheme();

  return (
    <div className={`settings-menu-overlay ${isOpen ? 'open' : ''}`} onClick={onClose}>
        <div className={`settings-menu`} onClick={(e) => e.stopPropagation()}>
            <div className="settings-menu-header">
                <h2>Settings</h2>
                <button onClick={onClose} className="close-button">&times;</button>
            </div>
            <div className="settings-menu-content">
                <ThemeToggler theme={theme} onToggle={toggleTheme} />
            </div>
        </div>
    </div>
  );
};

export default SettingsMenu;
