import { Component, ViewChild } from '@angular/core';
import { UrlFormComponent } from "./components/url-form/url-form.component";
import { UrlListComponent } from "./components/url-list/url-list.component";

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [UrlFormComponent, UrlListComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'tinyurl';
  @ViewChild(UrlListComponent) urlListComponent!: UrlListComponent;

  onUrlCreated() {
    this.urlListComponent.loadPublicUrls();
  }
}
