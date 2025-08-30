import React, { useState } from 'react';
import ReactMarkdown from 'react-markdown';
import './App.css';

function App() {
  const [message, setMessage] = useState('');
  const [inputValue, setInputValue] = useState('');

  const fetchData = async () => {
    // You can use the inputValue state here when making an API call
    console.log('Sending message:', inputValue);

    setMessage(''); // Clear previous message
    const response = await fetch('/api/data'); // This is a placeholder
    const reader = response.body?.getReader();
    const decoder = new TextDecoder();

    if (reader) {
      const read = async () => {
        const { done, value } = await reader.read();
        if (done) {
          return;
        }
        const chunk = decoder.decode(value, { stream: true });
        setMessage(prevMessage => prevMessage + chunk);
        read();
      };
      read();
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setInputValue(e.target.value);
  };

  const handleKeyPress = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      fetchData();
    }
  };


  return (
    <div className="App">
      <div className="markdown-container">
        <ReactMarkdown>{message}</ReactMarkdown>
      </div>
      <div className="input-area">
        <input
          type="text"
          value={inputValue}
          onChange={handleInputChange}
          onKeyPress={handleKeyPress}
          placeholder="Type your message here..."
        />
        <button onClick={fetchData}>Send</button>
      </div>
    </div>
  );
}

export default App;