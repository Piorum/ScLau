import React, { useState } from 'react';
import ReactMarkdown from 'react-markdown';
import './App.css';

function App() {
  const [message, setMessage] = useState('');
  const [inputValue, setInputValue] = useState('');

  const fetchData = async () => {
    console.log('Sending message:', inputValue);

    const currentInputValue = inputValue; // Store current input value
    setInputValue(''); // Clear the input box immediately
    setMessage('Starting request...'); // Set temporary message

    try {
      const response = await fetch('/api/data', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(currentInputValue), // Use stored value
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      setMessage(''); // Clear temporary message before streaming starts

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
    } catch (error) {
      console.error('Error sending data:', error);
      setMessage(`Error: ${error instanceof Error ? error.message : String(error)}`);
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