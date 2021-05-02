import { Component } from "@angular/core";
import { Square } from "src/models/square";
import { SquareChangeRequest } from "src/models/square-change-request";
import { WebSocketService } from "src/web-socket.service";

@Component({
  selector: "app-root",
  templateUrl: "./app.component.html",
  styleUrls: ["./app.component.scss"],
})
export class AppComponent {
  announcementSub;
  messages: string[] = [];
  squares: Square[] = [];
  colors: string[] = ["red", "green", "blue"];
  currentColor: string = "red";
  name: string = "";
  constructor(private socketService: WebSocketService) {
    this.socketService.announcement$.subscribe((announcement) => {
      if (announcement) {
        this.messages.unshift(announcement);
      }
    });
    this.socketService.squares$.subscribe((sq) => {
      this.squares = sq;
    });
    this.socketService.name$.subscribe((n) => {
      this.name = n;
    });
  }

  ngOnInit() {
    this.socketService.startSocket();
  }

  squareClick(event, square: Square) {
    if (square.Color === this.currentColor) return;
    var req = new SquareChangeRequest();
    req.Id = square.Id;
    req.Color = this.currentColor;
    this.socketService.sendSquareChangeRequest(req);
  }
}
