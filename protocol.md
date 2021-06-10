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

## Serialization and encoding

All serialized object mentioned in the protocol are done so in given manner:

The serialization is done by sequentially serializing basic data types using a [BinaryWriter](https://docs.microsoft.com/pl-pl/dotnet/api/system.io.binarywriter?view=netstandard-2.1).
The bare binary data is then converted to base64 string with no newlines.

Additionally some string are **encoded** so to not break with spaces inside them.
The encoding (later called **string-encoding**):

- `String` is converted to binary data through UTF-8 encoding.
- binary data is then encoded to `String` using base 64 conversion

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

- `register <login> <encoded_username> <encoded_password>` - where `<encoded_username>` is **string-encoded** username, and `<encoded_password>` is **string-encoded** password.

Server replies:

- `register ok <serialized_user>` - where `<serialized_user>` is serialized authenticated user object.
- `register err` - in case register command is not in proper format, registration was not successful or there is already user with given login or username.

After registration new user is automatically authenticated.

### Authenticate command

Client sends:

- `authenticate <login> <encoded_password>` - where `<encoded_password>` is **string-encoded** password.

Server replies:

- `authenticate ok <serialized_user>` - where `<serialized_user>` is serialized authenticated user object.
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

- `get groups <serialized_groups>` - where `<serialized_groups>` is a serialized collection of groups currently authenticated user is a member of.
- `get groups err` - when command is sent by an unauthenticated user.

### Get users command

Client sends:

- `get users <groupId>` - where `<groupId>` is the ID of a group, for which user wants to get members of.

Server replies:

- `get users <groupId> <serialized_users>` - where `<serialized_users>` is a serialized collection of users in group with given `<groupId>`.
- `get users <groupId> err` - in case if group with given `<groupId>` can't be found or the user is unauthenticated.

### Get messages command

Client sends:

- `get messages <groupId>` - where `<groupId>` is the ID of a group, for which user wants to get messages of

Server replies:

- `get messages <groupId> <serialized_messages>` - where `<serialized_messages>` is a serialized collection of messages in group with given `<groupId>`.
- `get messages <groupId> err` - in case if group with given `<groupId>` can't be found or the user is unauthenticated.

## New commands (sending data)

User can send data to server using various `new` commands.

Available `new` commands:

- [`message`](#New-message-command)
- [`group`](#New-group-command)

### New message command

Client sends:

- `new message <groupId> <encoded_content>` - where `<groupId`> is group identifier which is message destination, `<encoded_content>` is **string-encoded** message content.

Server does not reply to sender, but triggers `NewMessageEvent`, which then distributes this new message to all other connected clients in the group of this message.

### New group command

Client sends:

- `new group <encoded_name>` - where `<encoded_name>` is **string-encoded** name of the new group .

Server replies:

- `new group <serialized_group>` - where `<serialized_group>` is serialized created group object

- `new err` - if creating group was not successful

## Other

Miscellaneous command, as for [`joining`](#Join-command) a group.

### Join command

When client wants to join an existing group, he sends this command.

Client sends:

- `join <encoded_name>` - where `<encoded_name>` is **string-encoded** name of the group to join.

Server replies:

- `join <encoded_name> <serialized_group>` - where `<encoded_name>` is the **string-encoded** name of the group and `<serialized_group>` is serialized group which has been joined.
- `join <encoded_name> err` - where `<encoded_name>` is the **string-encoded** name of the group, when the group does not exists or the user is unauthenticated.

## Server notifications

Most actions are initiated by the client, but occasionally server needs to notify clients too.

### Server-side new message notification

Server redistributes a received message to all other clients in message's group by notifying them.

Server sends:

- `new message <serialized_message>` - where `<serialized_message>` is serialized message.

### Server-side new user notification

Server notifies all users in a specific group when a new member joins that group.

Server sends:

- `new user <groupId> <serialized_user>` - where `<groupId>` is the ID of the group, to which a new user has joined in and `<serialized_user>` is serialized user that has joined.
