import React from 'react';
import './ThemeToggler.css';

interface ThemeTogglerProps {
  theme: string;
  onToggle: () => void;
}

const ThemeToggler: React.FC<ThemeTogglerProps> = ({ theme, onToggle }) => {
  return (
    <div className="theme-toggler-container">
      <span className="theme-text">Dark Mode</span>
      <label className="switch">
        <input type="checkbox" onChange={onToggle} checked={theme === 'dark'} />
        <span className="slider round"></span>
      </label>
    </div>
  );
};

export default ThemeToggler;
