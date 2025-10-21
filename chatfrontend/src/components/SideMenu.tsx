import React, { useState, useRef, useEffect } from 'react';
import SettingsIcon from '../icons/SettingsIcon';
import './SideMenu.css';
import { ChatListItem } from '../types';
import NewChatIcon from '../icons/NewChatIcon';
import EditIcon from '../icons/EditIcon';
import DeleteIcon from '../icons/DeleteIcon';
import CheckmarkIcon from '../icons/CheckmarkIcon';
import XIcon from '../icons/XIcon';

interface SideMenuProps {
  isOpen: boolean;
  onClose: () => void;
  onSettingsClick: () => void;
  chats: ChatListItem[];
  onChatSelect: (chatId: string) => void;
  onNewChat: () => void;
  currentChatId: string | null;
  updateChatTitle: (chatId: string, newTitle: string) => void;
  deleteChat: (chatId: string) => void;
}

const SideMenu: React.FC<SideMenuProps> = ({ isOpen, onClose, onSettingsClick, chats, onChatSelect, onNewChat, currentChatId, updateChatTitle, deleteChat }) => {
  const [editingChatId, setEditingChatId] = useState<string | null>(null);
  const [editingTitle, setEditingTitle] = useState('');
  const editInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (editingChatId && editInputRef.current) {
      editInputRef.current.focus();
    }
  }, [editingChatId]);

  const handleEditClick = (chatId: string, currentTitle: string) => {
    setEditingChatId(chatId);
    setEditingTitle(currentTitle);
  };

  const handleSaveClick = (chatId: string) => {
    if (editingTitle.trim()) {
      updateChatTitle(chatId, editingTitle);
    }
    setEditingChatId(null);
    setEditingTitle('');
  };

  const handleCancelClick = () => {
    setEditingChatId(null);
    setEditingTitle('');
  };

  const handleDeleteClick = (chatId: string) => {
    deleteChat(chatId);
  };
  
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
              <div key={chat.chatId} className={`menu-button-container ${chat.chatId === currentChatId ? 'active' : ''}`}>
                {editingChatId === chat.chatId ? (
                  <div className="edit-chat-title-container">
                    <input 
                      ref={editInputRef}
                      type="text" 
                      value={editingTitle} 
                      onChange={(e) => setEditingTitle(e.target.value)} 
                      className="edit-chat-title-input"
                    />
                    <button onClick={() => handleSaveClick(chat.chatId)}><CheckmarkIcon /></button>
                    <button onClick={handleCancelClick}><XIcon /></button>
                  </div>
                ) : (
                  <button 
                    onClick={() => onChatSelect(chat.chatId)} 
                    className={`menu-button ${chat.chatId === currentChatId ? 'active' : ''}`}
                  >
                    <span className="menu-button-text">{chat.title || `Chat ${new Date(chat.lastMessage * 1000).toLocaleString()}`}</span>
                    <div className="menu-button-actions">
                      <button onClick={(e) => { e.stopPropagation(); handleEditClick(chat.chatId, chat.title || ''); }}><EditIcon /></button>
                      <button onClick={(e) => { e.stopPropagation(); handleDeleteClick(chat.chatId); }}><DeleteIcon /></button>
                    </div>
                  </button>
                )}
              </div>
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
