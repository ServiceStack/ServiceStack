const std = @import("std");

pub const WebServiceError = error{
    HttpError,
    SerializationError,
    DeserializationError,
    OutOfMemory,
};

pub const WebServiceException = struct {
    status_code: u16,
    status_description: []const u8,
    message: []const u8,
    response_status: ?std.json.Value = null,
};

pub const JsonServiceClient = struct {
    allocator: std.mem.Allocator,
    base_url: []const u8,
    reply_base_url: []const u8,
    oneway_base_url: []const u8,
    bearer_token: ?[]const u8 = null,
    username: ?[]const u8 = null,
    password: ?[]const u8 = null,
    http_client: std.http.Client,

    pub fn init(allocator: std.mem.Allocator, base_url: []const u8) !JsonServiceClient {
        const trimmed = std.mem.trimRight(u8, base_url, "/");
        const reply = try std.fmt.allocPrint(allocator, "{s}/api/", .{trimmed});
        const oneway = try std.fmt.allocPrint(allocator, "{s}/api/", .{trimmed});

        return JsonServiceClient{
            .allocator = allocator,
            .base_url = base_url,
            .reply_base_url = reply,
            .oneway_base_url = oneway,
            .http_client = std.http.Client{ .allocator = allocator },
        };
    }

    pub fn deinit(self: *JsonServiceClient) void {
        self.allocator.free(self.reply_base_url);
        self.allocator.free(self.oneway_base_url);
        self.http_client.deinit();
    }

    pub fn setBearerToken(self: *JsonServiceClient, token: []const u8) void {
        self.bearer_token = token;
    }

    pub fn setCredentials(self: *JsonServiceClient, user: []const u8, pass: []const u8) void {
        self.username = user;
        self.password = pass;
    }

    fn getTypeName(comptime T: type) []const u8 {
        if (@hasDecl(T, "get_type_name")) {
            return T.get_type_name();
        }
        const name = @typeName(T);
        if (std.mem.lastIndexOfScalar(u8, name, '.')) |idx| {
            return name[idx + 1 ..];
        }
        return name;
    }

    pub fn send(self: *JsonServiceClient, comptime ResponseType: type, method: std.http.Method, request_dto: anytype) !std.json.Parsed(ResponseType) {
        const type_name = getTypeName(@TypeOf(request_dto));
        const url = try std.fmt.allocPrint(self.allocator, "{s}{s}", .{ self.reply_base_url, type_name });
        defer self.allocator.free(url);

        return try self.sendUrl(ResponseType, method, url, request_dto);
    }

    pub fn sendUrl(self: *JsonServiceClient, comptime ResponseType: type, method: std.http.Method, url: []const u8, request_dto: anytype) !std.json.Parsed(ResponseType) {
        const uri = try std.Uri.parse(url);

        var headers = std.ArrayList(std.http.Header).empty;
        defer headers.deinit(self.allocator);

        try headers.append(self.allocator, .{ .name = "Accept", .value = "application/json" });

        var auth_buf: ?[]u8 = null;
        defer if (auth_buf) |b| self.allocator.free(b);

        if (self.bearer_token) |token| {
            auth_buf = try std.fmt.allocPrint(self.allocator, "Bearer {s}", .{token});
            try headers.append(self.allocator, .{ .name = "Authorization", .value = auth_buf.? });
        }

        var payload: ?[]u8 = null;
        defer if (payload) |p| self.allocator.free(p);

        if (method != .GET and method != .HEAD and method != .DELETE and method != .OPTIONS) {
            payload = try std.fmt.allocPrint(self.allocator, "{f}", .{std.json.fmt(request_dto, .{ .emit_null_optional_fields = false })});
            try headers.append(self.allocator, .{ .name = "Content-Type", .value = "application/json" });
        }

        var req = try self.http_client.request(method, uri, .{
            .extra_headers = headers.items,
        });
        defer req.deinit();

        if (payload) |p| {
            req.transfer_encoding = .{ .content_length = p.len };
            var body = try req.sendBodyUnflushed(&.{});
            try body.writer.writeAll(p);
            try body.end();
            try req.connection.?.flush();
        } else {
            try req.sendBodiless();
        }

        var redirect_buf: [8192]u8 = undefined;
        var res = try req.receiveHead(&redirect_buf);

        const arena = try self.allocator.create(std.heap.ArenaAllocator);
        arena.* = std.heap.ArenaAllocator.init(self.allocator);
        errdefer {
            arena.deinit();
            self.allocator.destroy(arena);
        }

        var body_buf: [4096]u8 = undefined;
        var reader = res.reader(&body_buf);

        const body_bytes = try reader.allocRemaining(arena.allocator(), .unlimited);

        if (@intFromEnum(res.head.status) >= 400) {
            return error.HttpError;
        }

        const value = try std.json.parseFromSliceLeaky(ResponseType, arena.allocator(), body_bytes, .{
            .ignore_unknown_fields = true,
        });

        return std.json.Parsed(ResponseType){
            .arena = arena,
            .value = value,
        };
    }

    pub fn get(self: *JsonServiceClient, comptime ResponseType: type, request_dto: anytype) !std.json.Parsed(ResponseType) {
        return self.send(ResponseType, .GET, request_dto);
    }

    pub fn post(self: *JsonServiceClient, comptime ResponseType: type, request_dto: anytype) !std.json.Parsed(ResponseType) {
        return self.send(ResponseType, .POST, request_dto);
    }

    pub fn put(self: *JsonServiceClient, comptime ResponseType: type, request_dto: anytype) !std.json.Parsed(ResponseType) {
        return self.send(ResponseType, .PUT, request_dto);
    }

    pub fn patch(self: *JsonServiceClient, comptime ResponseType: type, request_dto: anytype) !std.json.Parsed(ResponseType) {
        return self.send(ResponseType, .PATCH, request_dto);
    }

    pub fn delete(self: *JsonServiceClient, comptime ResponseType: type, request_dto: anytype) !std.json.Parsed(ResponseType) {
        return self.send(ResponseType, .DELETE, request_dto);
    }
};
