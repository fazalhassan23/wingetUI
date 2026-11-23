# System diagrams

The authoritative component and lifecycle diagrams are maintained in [architecture](architecture.md). This page documents the user-confirmation and recovery flows that cross presentation, application, and WinGet boundaries.

## Package operation flow

```mermaid
sequenceDiagram
    actor User
    participant View as MainWindow
    participant VM as MainViewModel
    participant Queue as PackageOperationQueue
    participant Adapter as WinGet adapter
    participant WinGet as App Installer

    User->>View: Select and review packages
    View->>User: Confirm IDs, versions, and source
    User->>View: Confirm
    View->>VM: Enqueue eligible snapshots
    VM->>Queue: Add non-duplicate entries
    loop One package at a time
        Queue->>Adapter: Execute confirmed snapshot
        Adapter->>WinGet: Re-resolve exact target
        alt State remains valid
            Adapter->>WinGet: Install or upgrade
            WinGet-->>Queue: Progress and result
        else Package state changed
            Adapter-->>Queue: Skipped
        end
    end
    Queue-->>VM: Completion
    VM->>Adapter: Refresh inventory
```

## Interrupted-operation recovery

```mermaid
flowchart TD
    A[Application starts] --> B[Load local activity JSON]
    B --> C{Entry unfinished?}
    C -- No --> D[Keep terminal result]
    C -- Yes --> E[Mark Unknown]
    E --> F[Explain interruption]
    D --> G[Refresh current inventory]
    F --> G
    G --> H[User reviews state before any retry]
```

WingetUI never infers success from process termination and never automatically retries an interrupted package operation.
