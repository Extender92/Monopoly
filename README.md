# Monopoly

Welcome to the Monopoly game project! This project is currently implemented as a console application,
but we are working towards a more modular architecture that separates core game functions from the frontend.
This will allow for different types of frontends, such as web, Windows, or mobile applications,
to interact with the core game logic without any direct dependencies.


## Project Structure

- **Monopoly.Core**: Contains the core game logic and functionalities. This is the heart of the application and is designed to be independent of any frontend implementation.
- **Monopoly.Console**: A console-based frontend that interacts with Monopoly.Core to provide a playable version of the game via console.
- **Monopoly.Tests**: Contains unit tests for ensuring the reliability and correctness of the game logic.

- **Monopoly.Web**: (Possible Future) A web-based frontend that will interact with Monopoly.Core to provide a playable version of the game via a web browser.
- **Monopoly.Windows**: (Possible Future) A Windows application frontend that will interact with Monopoly.Core to provide a playable version of the game via a Windows desktop application.


## Vision

The goal is to build a robust and flexible architecture where the core game logic can be reused across different types of frontends without modification.
This separation of concerns ensures that the core module (Monopoly.Core) does not need to know about the existence of any frontend implementations,
whether it's a console application, a web application, a Windows application, or any other type of user interface.


## Future Plans

- **Modular Frontends**: Implement web, Windows, and potentially mobile frontends that communicate with the core logic.
- **Enhanced Core Logic**: Continuously improve and extend the core game functionalities.
- **Comprehensive Testing**: Ensure high test coverage for all core functionalities to maintain reliability.


## Getting Started

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) installed on your machine.

### Running the Console Application

1. Clone the repository:
    ```bash
    git clone https://github.com/Extender92/Monopoly.git
    ```
2. Navigate to the console project directory:
    ```bash
    cd Monopoly/Monopoly.Console
    ```
3. Run the application:
    ```bash
    dotnet run
    ```


## Contact

For any questions or suggestions, feel free to reach out via [GitHub Issues](https://github.com/Extender92/Monopoly/issues).

---

This project is a fork of [CodeCraftersMR/CCMR-Monopoly](https://github.com/CodeCraftersMR/CCMR-Monopoly), and is moved here, I was the developer there and now continue to develop it here
