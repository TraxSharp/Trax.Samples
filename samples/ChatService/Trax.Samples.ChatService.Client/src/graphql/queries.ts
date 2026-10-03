import { gql } from "@apollo/client";

export const GET_CHAT_ROOMS = gql`
  query GetChatRooms {
    discover {
      getChatRooms {
        rooms {
          id
          name
          participantCount
          lastMessageAt
        }
      }
    }
  }
`;

export const GET_CHAT_HISTORY = gql`
  query GetChatHistory($input: GetChatHistoryInput!) {
    discover {
      getChatHistory(input: $input) {
        messages {
          id
          senderUserId
          senderDisplayName
          content
          sentAt
        }
      }
    }
  }
`;
