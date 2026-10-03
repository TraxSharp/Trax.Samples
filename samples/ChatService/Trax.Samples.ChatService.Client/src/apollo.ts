import {
  ApolloClient,
  InMemoryCache,
  HttpLink,
  split,
} from "@apollo/client";
import { GraphQLWsLink } from "@apollo/client/link/subscriptions";
import { getMainDefinition } from "@apollo/client/utilities";
import { createClient } from "graphql-ws";

const API_URL = "http://localhost:5210/trax/graphql";
const WS_URL = "ws://localhost:5210/trax/graphql";

export function createApolloClient(apiKey: string): ApolloClient<unknown> {
  const httpLink = new HttpLink({
    uri: API_URL,
    headers: { "X-Api-Key": apiKey },
  });

  const wsLink = new GraphQLWsLink(
    createClient({
      url: WS_URL,
      // Browsers cannot set headers on a WebSocket upgrade, so the key travels in the
      // connection_init payload. Trax reads it from "apiKey" (or "authToken").
      connectionParams: { apiKey },
    }),
  );

  const link = split(
    ({ query }) => {
      const definition = getMainDefinition(query);
      return (
        definition.kind === "OperationDefinition" &&
        definition.operation === "subscription"
      );
    },
    wsLink,
    httpLink,
  );

  return new ApolloClient({
    link,
    cache: new InMemoryCache(),
  });
}
