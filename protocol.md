# Communication Protocol

Socket connection is automatic and encryption is applied from the very beginning.

## Authentication

Client sends:
`authenticate login password`

Server replies either:

- `ok` and then immediately `{user_id, username}`
- `err`

The logged in user should be saved on both ends of the communication

## Getting groups

Client>
`get groups`

Server>
`unauthenticated`
or
`ok`


