import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class UrlService {

  private api = environment.apiUrl;

  constructor(private http: HttpClient) { }

  shorten(originalURL: string, isPrivate: boolean): Observable<any> {
    return this.http.post(`${this.api}/add`, { originalURL, isPrivate });
  }
}
