# tortuga

HTTP server built in C#

## Quick Start

Place static files in _assets folder and run ```dotnet run --Configuration release``` for production-ready HTTP server.  

Encryption is not yet built, so "production-ready" is a bit of a misnomer.

## Troubleshooting

- There is an issue with the opening and closing the ports on Windows servers; if you consistently receive 503 errors, try clearing the urlacl of all URLs for your given port using the following admin batch command as a template: 
```
netsh http delete urlacl url=http://*:8000/
```
    - if you're unsure what URLs are already being listened on, try running this command:
        ```
        netsh http show urlacl
        ```

## License

This project uses the MIT License.
