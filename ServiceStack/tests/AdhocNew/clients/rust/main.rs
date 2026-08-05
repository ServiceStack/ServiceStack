mod dtos;

use dtos::*;
use servicestack::blocking::JsonServiceClient;
use serde_json::json;

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut client = JsonServiceClient::new("http://localhost:5000");
    client.set_bearer_token("ak-87949de37e894627a9f6173154e7cafa");

    let request = ChatCompletion {
        model: "openai/gpt-oss-120b".to_string(),
        messages: vec![AiMessage {
            role: "user".to_string(),
            content: Some(vec![json!({
                "type": "text",
                "text": "Capital of France?"
            })]),
            ..Default::default()
        }],
        ..Default::default()
    };

    let response = client.send(&request)?;
    println!("{}", serde_json::to_string_pretty(&response)?);

    Ok(())
}
