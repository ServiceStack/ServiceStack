#!/usr/bin/env dart

import 'dart:io';
import 'dart:typed_data';
import 'package:servicestack/client.dart';
import 'package:servicestack/inspect.dart';
import 'package:my_dart/dtos.dart';

Future<void> main() async {
    var client = ClientFactory.api("http://localhost:5166");
    client.bearerToken = "ak-87949de37e894627a9f6173154e7cafa";
    
    var response = await client.send(ChatCompletion()
        ..model = "gemini-flash-latest"
        ..messages = [
            AiMessage()
                ..role = "user"
                ..content = [AiTextContent()
                    ..type = "text"
                    ..text = "Capital of France?"
                ]
        ]
        ..max_completion_tokens = 50
    );
    
    Inspect.printDump(response);
}
