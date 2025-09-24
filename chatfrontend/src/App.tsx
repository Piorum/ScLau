import React, { useState, useRef, useEffect } from 'react';
import './App.css';
import './ChatMessage.css';
import './ChatHistory.css';
import ChatHistory from './ChatHistory';
import TopBar from './components/TopBar';
import SideMenu from './components/SideMenu';
import SettingsMenu from './components/SettingsMenu';
import { useChat } from './hooks/useChat';
import { useTheme } from './context/ThemeContext';

function App() {
  const [inputValue, setInputValue] = useState('');
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const { messages, isAiResponding, sendMessage, deleteMessage, editMessage } = useChat();
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const { theme } = useTheme();

  // Dynamically load highlight.js theme
  useEffect(() => {
    const link = document.createElement('link');
    link.rel = 'stylesheet';
    link.href = theme === 'dark' ? 'highlight.js/styles/github-dark.css' : 'highlight.js/styles/github.css';
    document.head.appendChild(link);

    return () => {
      document.head.removeChild(link);
    };
  }, [theme]);

  // Auto-resize textarea height
  useEffect(() => {
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
      textareaRef.current.style.height = `${textareaRef.current.scrollHeight}px`;
    }
  }, [inputValue]);

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
    if (!inputValue.trim()) return;
    sendMessage(inputValue);
    setInputValue('');
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    setInputValue(e.target.value);
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault(); // Prevent default newline
      handleSendMessage();
    } else if (e.key === 'Enter' && e.shiftKey) {
      // Allow default newline behavior for Shift+Enter
    }
  };

  return (
    <div className="App">
      <SideMenu isOpen={isMenuOpen} onClose={closeMenu} onSettingsClick={openSettings} />
      <SettingsMenu isOpen={isSettingsOpen} onClose={closeSettings} />
      <div className="main-content-wrapper">
        <TopBar onMenuToggle={toggleMenu} />
        <div className="chat-area-wrapper">
            <ChatHistory messages={messages} isAiResponding={isAiResponding} deleteMessage={deleteMessage} editMessage={editMessage} />
            <div className="input-area">
              <textarea
                ref={textareaRef}
                value={inputValue}
                onChange={handleInputChange}
                onKeyDown={handleKeyDown}
                placeholder="Message gpt-oss:20b"
                rows={1}
              />
              <button onClick={handleSendMessage} disabled={!inputValue.trim()}>Send</button>
            </div>
        </div>
      </div>
    </div>
  );
}

export default App;