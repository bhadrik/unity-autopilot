// Licensed under the MIT License. See LICENSE in the project root for license information.

var UnityWebSocketLibrary = {
  /**
   * Pointer index for WebSocket objects.
   */
  $ptrIndex: 0,
  /**
   * Array of instanced WebSocket objects.
   */
  $webSockets: [],
  /**
   * Create a new WebSocket instance and adds it to the $webSockets array.
   * @param {string} urlPtr - A pointer to the URL string of connection.
   * @param {string[]} subProtocolsPtr - a pointer to a json array of strings that indicate the sub-protocols the client is willing to speak.
   * @param {function} onOpenCallback - The callback function. WebSocket_OnOpenDelegate(IntPtr websocketPtr) in C#.
   * @param {function} onMessageCallback - The callback function. WebSocket_OnMessageDelegate(IntPtr websocketPtr, IntPtr data, int length, int type) in C#.
   * @param {function} onErrorCallback - The callback function. WebSocket_OnErrorDelegate(IntPtr websocketPtr, IntPtr messagePtr) in C#.
   * @param {function} onCloseCallback - The callback function. WebSocket_OnCloseDelegate(IntPtr websocketPtr, int code, IntPtr reasonPtr) in C#.
   * @returns {number} - A pointer to the WebSocket instance, IntPtr in C#.
   */
  WebSocket_Create: function (urlPtr, subProtocolsPtr, onOpenCallback, onMessageCallback, onErrorCallback, onCloseCallback) {
    const url = UTF8ToString(urlPtr);
    try {
      for (let i = 0; i < webSockets.length; i++) {
        const instance = webSockets[i];
        if (instance !== undefined && instance.url !== undefined && instance.url === url) {
          throw new Error(`WebSocket connection already exists for URL: ${url}`);
        }
      }
      const socketPtr = ++ptrIndex;
      webSockets[socketPtr] = {
        socket: null,
        url: url,
        onOpenCallback: onOpenCallback,
        onMessageCallback: onMessageCallback,
        onErrorCallback: onErrorCallback,
        onCloseCallback: onCloseCallback
      };
      const subprotocolsStr = UTF8ToString(subProtocolsPtr);
      const subProtocols = JSON.parse(subprotocolsStr);
      if (subProtocols && Array.isArray(subProtocols)) {
        webSockets[socketPtr].subProtocols = subProtocols;
      } else {
        throw new Error(`subProtocols is not an array: ${subprotocolsStr}`);
      }
      // console.log(`Created WebSocket object with websocketPtr: ${socketPtr} for URL: ${url}, sub-protocols: ${subProtocols}`);
      return socketPtr;
    } catch (error) {
      console.error(`Error creating WebSocket object for URL: ${url} Error: ${error}`);
      return 0;
    }
  },
  /**
   * Get the current state of the WebSocket connection.
   * @param socketPtr - A pointer to the WebSocket object. IntPtr in C#.
   * @returns {number} - The current state of the WebSocket connection.
   */
  WebSocket_GetState: function (socketPtr) {
    try {
      const instance = webSockets[socketPtr];
      if (!instance || !instance.socket) { return 0; }
      return instance.socket.readyState;
    } catch (error) {
      console.error(`Error getting WebSocket state for websocketPtr: ${socketPtr} Error: ${error}`);
      return 3;
    }
  },
  /**
   * Connect the WebSocket connection.
   * @param socketPtr - A pointer to the WebSocket object. IntPtr in C#.
   */
  WebSocket_Connect: function (socketPtr) {
    try {
      const instance = webSockets[socketPtr];
      if (!instance) {
        throw new Error(`WebSocket instance not found for websocketPtr: ${socketPtr}`);
      }
      if (!instance.subProtocols || instance.subProtocols.length === 0) {
        instance.socket = new WebSocket(instance.url);
      } else {
        instance.socket = new WebSocket(instance.url, instance.subProtocols);
      }
      instance.socket.binaryType = 'arraybuffer';
      instance.socket.onopen = function () {
        try {
          // console.log(`WebSocket connection opened for websocketPtr: ${socketPtr}`);
          Module.dynCall_vi(instance.onOpenCallback, socketPtr);
        } catch (error) {
          console.error(`Error calling onOpen callback for websocketPtr: ${socketPtr} Error: ${error}`);
        }
      };
      instance.socket.onmessage = function (event) {
        try {
          // console.log(`Received message for websocketPtr: ${socketPtr} with data: ${event.data}`);
          if (event.data instanceof ArrayBuffer) {
            const array = new Uint8Array(event.data);
            const buffer = _malloc(array.length);
            writeArrayToMemory(array, buffer);
            try {
              Module.dynCall_viiii(instance.onMessageCallback, socketPtr, buffer, array.length, 1);
            } finally {
              _free(buffer);
            }
          } else if (typeof event.data === 'string') {
            const length = lengthBytesUTF8(event.data) + 1;
            const buffer = _malloc(length);
            stringToUTF8(event.data, buffer, length);
            try {
              Module.dynCall_viiii(instance.onMessageCallback, socketPtr, buffer, length, 0);
            } finally {
              _free(buffer);
            }
          } else {
            console.error(`Error parsing message for websocketPtr: ${socketPtr} with data: ${event.data}`);
          }
        } catch (error) {
          console.error(`Error calling onMessage callback for websocketPtr: ${socketPtr} Error: ${error}`);
        }
      };
      instance.socket.onerror = function (event) {
        try {
          console.error(`WebSocket error for websocketPtr: ${socketPtr} with message: ${event}`);
          const json = JSON.stringify(event);
          const length = lengthBytesUTF8(json) + 1;
          const buffer = _malloc(length);
          stringToUTF8(json, buffer, length);
          try {
            Module.dynCall_vii(instance.onErrorCallback, socketPtr, buffer);
          } finally {
            _free(buffer);
          }
        } catch (error) {
          console.error(`Error calling onError callback for websocketPtr: ${socketPtr} Error: ${error}`);
        }
      };
      instance.socket.onclose = function (event) {
        try {
          // console.log(`WebSocket connection closed for websocketPtr: ${socketPtr} with code: ${event.code} and reason: ${event.reason}`);
          const length = lengthBytesUTF8(event.reason) + 1;
          const buffer = _malloc(length);
          stringToUTF8(event.reason, buffer, length);
          try {
            Module.dynCall_viii(instance.onCloseCallback, socketPtr, event.code, buffer);
          } finally {
            _free(buffer);
          }
        } catch (error) {
          console.error(`Error calling onClose callback for websocketPtr: ${socketPtr} Error: ${error}`);
        }
      };
      // console.log(`Connecting WebSocket connection for websocketPtr: ${socketPtr}`);
    } catch (error) {
      console.error(`Error connecting WebSocket connection for websocketPtr: ${socketPtr} Error: ${error}`);
    }
  },
  /**
   * Send data to the WebSocket connection.
   * @param socketPtr - A pointer to the WebSocket object. IntPtr in C#.
   * @param data - A pointer to the data to send.
   * @param length - The length of the data to send.
   */
  WebSocket_SendData: function (socketPtr, data, length) {
    try {
      const instance = webSockets[socketPtr];
      if (!instance || !instance.socket || instance.socket.readyState !== 1) {
        throw new Error(`WebSocket connection does not exist for websocketPtr: ${socketPtr}`);
      }
      // console.log(`Sending message to WebSocket connection for websocketPtr: ${socketPtr} with data: ${data} and length: ${length}`);
      instance.socket.send(new Uint8Array(Module.HEAPU8.subarray(data, data + length)));
    } catch (error) {
      console.error(`Error sending message to WebSocket connection for websocketPtr: ${socketPtr} Error: ${error}`);
    }
  },
  /**
   * Send a string to the WebSocket connection.
   * @param socketPtr - A pointer to the WebSocket object. IntPtr in C#.
   * @param data - The string to send.
   */
  WebSocket_SendString: function (socketPtr, data) {
    try {
      const instance = webSockets[socketPtr];
      if (!instance || !instance.socket || instance.socket.readyState !== 1) {
        throw new Error(`WebSocket connection does not exist for websocketPtr: ${socketPtr}`);
      }
      const dataStr = UTF8ToString(data);
      // console.log(`Sending message to WebSocket connection for websocketPtr: ${socketPtr} with data: ${dataStr}`);
      instance.socket.send(dataStr);
    } catch (error) {
      console.error(`Error sending message to WebSocket connection for websocketPtr: ${socketPtr} Error: ${error}`);
    }
  },
  /**
   * Close the WebSocket connection.
   * @param socketPtr - A pointer to the WebSocket object. IntPtr in C#.
   * @param code - The status code for the close.
   * @param reason - The reason for the close.
   */
  WebSocket_Close: function (socketPtr, code, reason) {
    try {
      const instance = webSockets[socketPtr];
      if (!instance || !instance.socket || instance.socket.readyState >= 2) {
        throw new Error(`WebSocket connection already closed for websocketPtr: ${socketPtr}`);
      }
      const reasonStr = UTF8ToString(reason);
      // console.log(`Closing WebSocket connection for websocketPtr: ${socketPtr} with code: ${code} and reason: ${reasonStr}`);
      instance.socket.close(code, reasonStr);
    } catch (error) {
      console.error(`Error closing WebSocket connection for websocketPtr: ${socketPtr} Error: ${error}`);
    }
  },
  /**
   * Destroy a WebSocket object.
   * @param socketPtr - A pointer to the WebSocket object. IntPtr in C#.
   */
  WebSocket_Dispose: function (socketPtr) {
    try {
      if (socketPtr === 0) { return; }
      const instance = webSockets[socketPtr];
      if (!instance) {
        throw new Error(`WebSocket instance not found for websocketPtr: ${socketPtr}`);
      }
      // console.log(`Disposing WebSocket object with websocketPtr: ${socketPtr}`);
      delete webSockets[socketPtr];
    } catch (error) {
      console.error(`Error disposing WebSocket object with websocketPtr: ${socketPtr} Error: ${error}`);
    }
  }
};
autoAddDeps(UnityWebSocketLibrary, '$ptrIndex');
autoAddDeps(UnityWebSocketLibrary, '$webSockets');
mergeInto(LibraryManager.library, UnityWebSocketLibrary);
