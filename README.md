# Parallelization-Project
TCP server written in C# designed for image sharing

## Features
* **Multi-threaded Server:** Handles multiple client connections simultaneously without blocking.
* **Image Upload:** Clients can upload images to the server's storage.
* **Image Download:** Clients can browse and download images available on the server.
* **Command-Based Interface:** Clean CLI using the **Command Design Pattern** for extensibility.
* **Thread-Safe Logging:** Server activities are recorded with timestamps in `serverLog.txt`.
* **Robust Error Handling:** Manages disconnections and file transfer interruptions gracefully.
* **Configurable:** Network settings (IP/Port) are managed via `App.config`.

## Installation & Run

1. **Download & Extract**
   * Open the GitHub repository.
   * Click **<> Code** and select **Download ZIP**.
   * Extract the downloaded ZIP file to a folder on your computer.

2. **Locate the Executables**
   To run the application, navigate to the output folder inside the project directories:

   * **For the Server:**
     Go to: `Server/bin/Debug/net8.0/`
     Run: `Server.exe`

   * **For the Client:**
     Go to: `TcpClient/bin/Debug/net8.0/`
     Run: `TcpClient.exe`

## ⚙️ Configuration

Both the Server and Client applications use an `App.config` file to define network settings. Ensure both are set to the same port.

```xml
<configuration>
  <appSettings>
    <add key="IpAddress" value="127.0.0.1" />
    <add key="Port" value="8080" />
  </appSettings>
</configuration>
```
## ❓ Help & Support

If you run into any issues with the installation or have questions about the implementation, feel free to reach out.

* **Email:** [krejci3@spsejecna.cz](mailto:krejci3@spsejecna.cz)
* **Issues:** If you find a bug, please open an issue in this repository.

## License

This project is open-source and intended for **educational and personal purposes**.

Distributed under the **MIT License**. You are free to modify and distribute this software as long as the original credit is maintained.
