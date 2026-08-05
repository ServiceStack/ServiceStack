use serde::de::DeserializeOwned;
use serde::Serialize;
use serde_json::Value;
use std::collections::HashMap;
use std::fmt;

#[derive(Debug)]
pub struct WebServiceException {
    pub status_code: u16,
    pub status_description: String,
    pub message: String,
    pub response_status: Option<Value>,
}

impl fmt::Display for WebServiceException {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        write!(
            f,
            "HTTP {} {}: {}",
            self.status_code, self.status_description, self.message
        )
    }
}

impl std::error::Error for WebServiceException {}

pub struct JsonServiceClient {
    pub base_url: String,
    pub reply_base_url: String,
    pub oneway_base_url: String,
    pub bearer_token: Option<String>,
    pub username: Option<String>,
    pub password: Option<String>,
    pub headers: HashMap<String, String>,
}

impl JsonServiceClient {
    pub fn new(base_url: &str) -> Self {
        let mut client = Self {
            base_url: base_url.trim_end_matches('/').to_string(),
            reply_base_url: String::new(),
            oneway_base_url: String::new(),
            bearer_token: None,
            username: None,
            password: None,
            headers: HashMap::new(),
        };
        client.set_base_path("api");
        client
    }

    pub fn set_base_path(&mut self, base_path: &str) -> &mut Self {
        if base_path.is_empty() {
            self.reply_base_url = format!("{}/json/reply/", self.base_url);
            self.oneway_base_url = format!("{}/json/oneway/", self.base_url);
        } else {
            let path = base_path.trim_matches('/');
            self.reply_base_url = format!("{}/{}/", self.base_url, path);
            self.oneway_base_url = format!("{}/{}/", self.base_url, path);
        }
        self
    }

    pub fn set_bearer_token(&mut self, token: &str) -> &mut Self {
        self.bearer_token = Some(token.to_string());
        self
    }

    pub fn set_credentials(&mut self, user: &str, pass: &str) -> &mut Self {
        self.username = Some(user.to_string());
        self.password = Some(pass.to_string());
        self
    }

    pub fn post<R: DeserializeOwned, T: Serialize>(
        &self,
        request: &T,
    ) -> Result<R, WebServiceException> {
        self.send("POST", request)
    }

    pub fn get<R: DeserializeOwned, T: Serialize>(
        &self,
        request: &T,
    ) -> Result<R, WebServiceException> {
        self.send("GET", request)
    }

    pub fn put<R: DeserializeOwned, T: Serialize>(
        &self,
        request: &T,
    ) -> Result<R, WebServiceException> {
        self.send("PUT", request)
    }

    pub fn patch<R: DeserializeOwned, T: Serialize>(
        &self,
        request: &T,
    ) -> Result<R, WebServiceException> {
        self.send("PATCH", request)
    }

    pub fn delete<R: DeserializeOwned, T: Serialize>(
        &self,
        request: &T,
    ) -> Result<R, WebServiceException> {
        self.send("DELETE", request)
    }

    pub fn send<R: DeserializeOwned, T: Serialize>(
        &self,
        method: &str,
        request: &T,
    ) -> Result<R, WebServiceException> {
        let dto_name = std::any::type_name::<T>()
            .split("::")
            .last()
            .unwrap_or("Request");

        let mut url = format!("{}{}", self.reply_base_url, dto_name);

        let has_body = !matches!(
            method.to_uppercase().as_str(),
            "GET" | "DELETE" | "HEAD" | "OPTIONS"
        );

        if !has_body {
            if let Ok(val) = serde_json::to_value(request) {
                if let Some(obj) = val.as_object() {
                    let mut query_params = Vec::new();
                    for (k, v) in obj {
                        if !v.is_null() {
                            let str_val = match v {
                                Value::String(s) => s.clone(),
                                Value::Bool(b) => b.to_string(),
                                _ => v.to_string(),
                            };
                            query_params.push(format!(
                                "{}={}",
                                urlencoding_encode(k),
                                urlencoding_encode(&str_val)
                            ));
                        }
                    }
                    if !query_params.is_empty() {
                        let sep = if url.contains('?') { "&" } else { "?" };
                        url = format!("{}{}{}", url, sep, query_params.join("&"));
                    }
                }
            }
        }

        self.send_url(method, &url, if has_body { Some(request) } else { None })
    }

    pub fn send_url<R: DeserializeOwned, T: Serialize>(
        &self,
        method: &str,
        url: &str,
        body: Option<&T>,
    ) -> Result<R, WebServiceException> {
        let mut req = match method.to_uppercase().as_str() {
            "GET" => ureq::get(url),
            "POST" => ureq::post(url),
            "PUT" => ureq::put(url),
            "PATCH" => ureq::patch(url),
            "DELETE" => ureq::delete(url),
            "OPTIONS" => ureq::request("OPTIONS", url),
            "HEAD" => ureq::head(url),
            _ => ureq::post(url),
        };

        req = req.set("Accept", "application/json");

        if let Some(ref token) = self.bearer_token {
            req = req.set("Authorization", &format!("Bearer {}", token));
        }

        for (k, v) in &self.headers {
            req = req.set(k, v);
        }

        let response_result = if let Some(b) = body {
            req.send_json(b)
        } else {
            req.call()
        };

        match response_result {
            Ok(response) => {
                response.into_json::<R>().map_err(|e| WebServiceException {
                    status_code: 500,
                    status_description: "Deserialization Error".to_string(),
                    message: e.to_string(),
                    response_status: None,
                })
            }
            Err(ureq::Error::Status(code, response)) => {
                let status_text = response.status_text().to_string();
                let body_str = response.into_string().unwrap_or_default();
                let resp_status = serde_json::from_str::<Value>(&body_str).ok();

                Err(WebServiceException {
                    status_code: code,
                    status_description: status_text.clone(),
                    message: format!("HTTP {} {}: {}", code, status_text, body_str),
                    response_status: resp_status,
                })
            }
            Err(e) => Err(WebServiceException {
                status_code: 500,
                status_description: "Network Error".to_string(),
                message: e.to_string(),
                response_status: None,
            }),
        }
    }
}

fn urlencoding_encode(s: &str) -> String {
    s.chars()
        .map(|c| match c {
            'A'..='Z' | 'a'..='z' | '0'..='9' | '-' | '_' | '.' | '~' => c.to_string(),
            _ => format!("%{:02X}", c as u8),
        })
        .collect()
}
