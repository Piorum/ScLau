export interface StreamedData {
  response: string | null;
  channel: string | null;
}

/**
 * ApiStreamParser is a utility class for safely parsing JSON strings received from a streaming API.
 * It handles potential parsing errors and provides methods to extract specific data fields
 * with type safety and default fallbacks.
 */
export class ApiStreamParser {
  private readonly data: any;

  /**
   * Constructs an ApiStreamParser instance.
   * @param jsonString The JSON string to parse. Handles cases where the string might be invalid JSON.
   */
  constructor(jsonString: string) {
    try {
      this.data = JSON.parse(jsonString);
    } catch (e) {
      // If parsing fails, initialize data as an empty object to prevent further errors
      // and log the parsing error for debugging.
      this.data = {};
      // console.error('Error parsing JSON:', e, 'Line:', jsonString); // Removed console.error for cleanup
    }
  }

  /**
   * Extracts and returns the 'response' and 'channel' fields from the parsed data.
   * Ensures channel is always lowercase for consistent comparison.
   * @returns An object containing the response text and channel name, or null if not found.
   */
  public getData(): StreamedData {
    const response = this.getString('response');
    const channel = this.getString('channel')?.toLowerCase() ?? null;

    return { response, channel };
  }

  /**
   * Safely retrieves a string value from the parsed data by key.
   * Handles cases where the value might be missing, not a string, or a number (converting numbers to strings).
   * @param key The key of the property to retrieve.
   * @returns The string value, or null if the property is not a string or number, or is missing.
   */
  private getString(key: string): string | null {
    if (this.data && typeof this.data[key] === 'string') {
      return this.data[key];
    }
    // Handle cases where the backend might send an enum as a number.
    if (this.data && typeof this.data[key] === 'number') {
        return String(this.data[key]);
    }
    return null;
  }
}