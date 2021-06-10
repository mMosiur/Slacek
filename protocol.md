# Communication Protocol

Communication starts with cryptographic [handshake](#Handshake).

Then all following communication is command-based.

Available top-level commands:

- [ping](#Ping-command)
- [register](#Register-command)
- [authenticate](#Authenticate-command)
- [unauthenticate](#Unathenticate-command)
- [get](#Get-commands-requesting-data)
- [new](#New-commands-sending-data)
- [join](#Join-command)

## Handshake

Socket connection is automatic and encryption is applied from the very beginning.

![Tunnel diagram](Diagrams/tunnel.png)

## Serialization

All serialized object mentioned in the protocol are done so in given manner:

The serialization is done by sequentially serializing basic data types using a [BinaryWriter](https://docs.microsoft.com/pl-pl/dotnet/api/system.io.binarywriter?view=netstandard-2.1).
The bare binary data is then converted to base64 string with no newlines.

### User serialization

Serialized user consists of serialized:

1. `User.UserId` (`Int32`)
2. `User.Username` (`String`)

in specified order.

### Group serialization

Serialized group consists of serialized:

1. `Group.GroupId` (`Int32`)
2. `Group.Name` (`String`)

in specified order.

### Message serialization

Serialized message consists of serialized:

1. `Message.GroupId` (`Int32`)
2. `Message.UserId` (`Int32`)
3. `Message.Content` (`String`)

in specified order.

### Collection serialization

Collection serialization is possible to any of previously mentioned class models: `User`, `Group` or `Message` (later called simply a `Model`).

Serialized collection of `Model` type object consists of serialized:

1. `Collection.Count` (`Int32`)
2. [...] `Model` for each `Model` in `Collection.Items`

where every element of the collection is serialized sequentially.

## Health-check

Client can check wether server is live and responding using [ping command](#Ping-command).

### Ping command

Client sends:

1. `ping`
2. `ping <message>` - where message is any text provided by client

Server replies:

1. `pong`
2. `pong <message>` - if message given in ping command, server returns it with pong

## User authentication

Basic mechanism is implemented to authenticate users.
A user can request authentication (login) with [authenticate command](#Authenticate-command), log out with [unauthenticate command](#Unauthenticate-command) or register with [register command](#Register-command).

Both sides keep track of currently authenticated user in given session.

### Register command

Client sends:

- `register <login> <username> <password>`

Server replies:

- `register ok <login> <username> <password>`
- `register err` - in case register command is not in proper format, registration was not successful or there is already user with given login or username.

After registration new user is automatically authenticated.

### Authenticate command

Client sends:

- `authenticate <login> <password>`

Server replies:

- `authenticate ok <payload>` - where payload is serialized authenticated user object.
- `authenticate err` - in case command is not in proper format, authentication did not succeed or there was an database error.

### Unauthenticate command

Client sends:

- `unauthenticate`

Server does not reply.

Both sides change the session state to unauthenticated.

## Get commands (requesting data)

User can request data from server using various `get` commands.

Available `get` commands:

- [`groups`](#Get-groups-command)
- [`users`](#Get-users-command)
- [`messages`](#Get-messages-command)

### Get groups command

Client sends:

- `get groups`

Server replies:

- `get groups <payload>` - where `<payload>` is a serialized collection of groups currently authenticated user is a member of.
- `get groups err` - when command is sent by an unauthenticated user.

### Get users command

Client sends:

- `get users <groupId>` - where `<groupId>` is the ID of a group, for which user wants to get members of.

Server replies:

- `get users <groupId> <payload>` - where `<payload>` is a serialized collection of users in group with given `<groupId>`.
- `get users <groupId> err` - in case if group with given `<groupId>` can't be found or the user is unauthenticated.

### Get messages command

Client sends:

- `get messages <groupId>` - where `<groupId>` is the ID of a group, for which user wants to get messages of

Server replies:

- `get messages <groupId> <payload>` - where `<payload>` is a serialized collection of messages in group with given `<groupId>`.
- `get messages <groupId> err` - in case if group with given `<groupId>` can't be found or the user is unauthenticated.

## New commands (sending data)

User can send data to server using various `new` commands.

Available `new` commands:

- [`message`](#New-message-command)
- [`group`](#New-group-command)

### New message command

Client sends:

- `new message <groupId> <payload>` - where `<groupId`> is group identifier which is message destination, `<payload>` encoded message content (string ➡ UTF-8 binary encoding ➡ base 64 string).

Server does not reply to sender, but triggers `NewMessageEvent`, which then distributes this new message to all other connected clients in the group of this message.

### New group command

Client sends:

- `new group <payload>` - where payload is encoded name of the new group (string ➡ UTF-8 binary encoding ➡ base 64 string).

Server replies:

- `new group <payload>` - where `<payload>` is serialized created group object

- `new err` - if creating group was not successful

## Other

Miscellaneous command, as for [`joining`](#Join-command) a group.

### Join command

When client wants to join an existing group, he sends this command.

Client sends:

- `join <payload>` - where `<payload>` is encoded name of the group to join (string ➡ UTF-8 binary encoding ➡ base 64 string).

Server replies:

- `join <payload1> <payload2>` - where `<payload1>` is mirroring `<payload>` sent by client and `<payload2>` is serialized group which has been joined.
- `join <payload> err` - where `payload` is mirroring `<payload>` sent by client, when the specified group does not exists or the user is unauthenticated.

## Server notifications

Most actions are initiated by the client, but occasionally server needs to notify clients too.

### Server-side new message notification

Server redistributes a received message to all other clients in message's group by notifying them.

Server sends:

- `new message <payload>` - where `<payload>` is serialized message.

### Server-side new user notification

Server notifies all users in a specific group when a new member joins that group.

Server sends:

- `new user <groupId> <payload>` - where `<groupId>` is the ID of the group, to which a new user has joined in and `<payload>` is serialized user that has joined.
