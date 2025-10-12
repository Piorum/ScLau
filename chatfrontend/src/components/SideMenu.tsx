import React from 'react';
import SettingsIcon from '../icons/SettingsIcon';
import './SideMenu.css';
import { ChatListItem } from '../types';
import NewChatIcon from '../icons/NewChatIcon';

interface SideMenuProps {
  isOpen: boolean;
  onClose: () => void;
  onSettingsClick: () => void;
  chats: ChatListItem[];
  onChatSelect: (chatId: string) => void;
  onNewChat: () => void;
  currentChatId: string | null;
}

const SideMenu: React.FC<SideMenuProps> = ({ isOpen, onClose, onSettingsClick, chats, onChatSelect, onNewChat, currentChatId }) => {
  return (
    <>
      <div className={`side-menu-overlay ${isOpen ? 'open' : ''}`} onClick={onClose}></div>
      <div className={`side-menu ${isOpen ? 'open' : ''}`}>
        <div className="side-menu-content">
          <button onClick={onNewChat} className = {`menu-button ${currentChatId === null ? 'active' : ''}`}>
            <span className="menu-button-text">New Chat</span>
            <NewChatIcon />
          </button>
          {chats
            .sort((a, b) => b.lastMessage - a.lastMessage)
            .map(chat => (
            <button key={chat.chatId} onClick={() => onChatSelect(chat.chatId)} className={`menu-button ${chat.chatId === currentChatId ? 'active' : ''}`}>
              <span className="menu-button-text">{chat.title || `Chat ${new Date(chat.lastMessage * 1000).toLocaleString()}`}</span>
            </button>
          ))}
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
