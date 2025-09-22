import React, { useState } from 'react';
import './App.css';
import './ChatMessage.css';
import './ChatHistory.css';
import ChatHistory from './ChatHistory';
import TopBar from './components/TopBar';
import SideMenu from './components/SideMenu';
import SettingsMenu from './components/SettingsMenu';
import { useChat } from './hooks/useChat';

function App() {
  const [inputValue, setInputValue] = useState('');
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const { messages, sendMessage } = useChat();

  const toggleMenu = () => {
    setIsMenuOpen(!isMenuOpen);
  };

  const closeMenu = () => {
    setIsMenuOpen(false);
  };

  const openSettings = () => {
    setIsSettingsOpen(true);
  };

  const closeSettings = () => {
    setIsSettingsOpen(false);
  };

  const handleSendMessage = () => {
    sendMessage(inputValue);
    setInputValue('');
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setInputValue(e.target.value);
  };

  const handleKeyPress = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      handleSendMessage();
    }
  };

  return (
    <div className="App">
      <SideMenu isOpen={isMenuOpen} onClose={closeMenu} onSettingsClick={openSettings} />
      <SettingsMenu isOpen={isSettingsOpen} onClose={closeSettings} />
      <div className="main-content-wrapper">
        <TopBar onMenuToggle={toggleMenu} />
        <div className="chat-area-wrapper">
            <ChatHistory messages={messages} />
            <div className="input-area">
              <input
                type="text"
                value={inputValue}
                onChange={handleInputChange}
                onKeyPress={handleKeyPress}
                placeholder="Type your message here..."
              />
              <button onClick={handleSendMessage} disabled={!inputValue.trim()}>Send</button>
            </div>
        </div>
      </div>
    </div>
  );
}

export default App;
